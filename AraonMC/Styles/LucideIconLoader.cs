using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;

namespace AraonMC.Styles;

/// <summary>
///   把 <c>Assets/Icons/lucide/*.svg</c> 加载成 <see cref="Geometry"/>，按 Lucide slug（文件名去掉
///   <c>.svg</c>）注册为应用资源。Lucide 的 path/circle/rect/polyline/polygon/line 元素被压平成
///   单条 Avalonia 路径数据，只保留描边轮廓——填色/描边色由 <c>&lt;Path&gt;</c> 的 Foreground 决定，
///   沿用主题。图标以 <c>.svg</c> 文件形式存放于仓库，不再手写 path 数据。
/// </summary>
internal static class LucideIconLoader
{
    private const string PackBase = "avares://AraonMC/Assets/Icons/lucide/";

    /// <summary>枚举图标包下全部 .svg，返回 (slug, geometry) 序列，供注册到应用资源。</summary>
    public static IEnumerable<(string Slug, Geometry Geometry)> Load()
    {
        foreach (var uri in AssetLoader.GetAssets(new Uri(PackBase), null))
        {
            if (!string.Equals(Path.GetExtension(uri.AbsolutePath), ".svg", StringComparison.OrdinalIgnoreCase))
                continue;

            var slug = Path.GetFileNameWithoutExtension(uri.AbsolutePath);
            var geom = ToGeometry(AssetLoader.Open(uri));
            if (geom is null) continue;

            yield return (slug, geom);
        }
    }

    /// <summary>把 Lucide SVG 的每个图形元素各建成一条独立 <see cref="Geometry"/>，装进一个 <see cref="GeometryGroup"/>。</summary>
    /// <remarks>每个元素单独解析，当前点都从 (0,0) 起——避免相对 <c>m</c> 开头的子路径在拼接时相对到上一条终点而错位。</remarks>
    private static Geometry? ToGeometry(Stream svg)
    {
        using (svg)
        using (var reader = XmlReader.Create(svg))
        {
            var group = new GeometryGroup { FillRule = FillRule.NonZero };
            while (reader.Read())
            {
                if (reader.NodeType != XmlNodeType.Element) continue;
                string? frag = reader.LocalName switch
                {
                    "path"     => reader.GetAttribute("d"),
                    "circle"   => Circle(reader),
                    "rect"     => Rect(reader),
                    "polyline" => Poly(reader, close: false),
                    "polygon"  => Poly(reader, close: true),
                    "line"     => Line(reader),
                    _          => null,
                };
                if (!string.IsNullOrWhiteSpace(frag))
                    group.Children.Add(Geometry.Parse(frag));
            }
            return group.Children.Count > 0 ? group : null;
        }
    }

    private static double Num(string? s)
        => double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 0;

    private static string F(double v) => v.ToString("0.######", CultureInfo.InvariantCulture);

    // 圆：从 (cx-r, cy) 起两段半圆弧。
    private static string Circle(XmlReader r)
    {
        var cx = Num(r.GetAttribute("cx"));
        var cy = Num(r.GetAttribute("cy"));
        var rad = Num(r.GetAttribute("r"));
        return $"M {F(cx - rad)},{F(cy)} a {F(rad)},{F(rad)} 0 1 0 {F(2 * rad)},0 a {F(rad)},{F(rad)} 0 1 0 {F(-2 * rad)},0";
    }

    // 矩形（可带圆角 rx）。
    private static string Rect(XmlReader r)
    {
        var x = Num(r.GetAttribute("x"));
        var y = Num(r.GetAttribute("y"));
        var w = Num(r.GetAttribute("width"));
        var h = Num(r.GetAttribute("height"));
        var rx = Num(r.GetAttribute("rx"));
        if (rx <= 0) rx = Num(r.GetAttribute("ry"));
        if (rx <= 0 || rx > w / 2 || rx > h / 2)
            return $"M {F(x)},{F(y)} h {F(w)} v {F(h)} h {F(-w)} z";

        var c = F(rx);
        var cdx = F(w - 2 * rx);
        var cdy = F(h - 2 * rx);
        var nc = F(-rx);
        return $"M {F(x + rx)},{F(y)} h {cdx} a {c},{c} 0 0 1 {c},{c} v {cdy} "
             + $"a {c},{c} 0 0 1 {nc},{c} h {F(-(w - 2 * rx))} "
             + $"a {c},{c} 0 0 1 {nc},{nc} v {F(-(h - 2 * rx))} a {c},{c} 0 0 1 {c},{nc} z";
    }

    // 折线/多边形：points 列表 → M x,y L x,y ...
    private static string Poly(XmlReader r, bool close)
    {
        var pts = (r.GetAttribute("points") ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(pts)) return string.Empty;
        var coords = pts.Split(new[] { ' ', ',', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        if (coords.Length < 2) return string.Empty;

        var sb = new StringBuilder().Append('M').Append(coords[0]).Append(',').Append(coords[1]);
        for (var i = 2; i + 1 < coords.Length; i += 2)
            sb.Append(" L").Append(coords[i]).Append(',').Append(coords[i + 1]);
        if (close) sb.Append(" Z");
        return sb.ToString();
    }

    private static string Line(XmlReader r)
    {
        var x1 = Num(r.GetAttribute("x1")); var y1 = Num(r.GetAttribute("y1"));
        var x2 = Num(r.GetAttribute("x2")); var y2 = Num(r.GetAttribute("y2"));
        return $"M {F(x1)},{F(y1)} L {F(x2)},{F(y2)}";
    }
}
