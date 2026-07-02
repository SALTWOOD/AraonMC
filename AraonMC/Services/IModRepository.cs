using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AraonMC.Models;

namespace AraonMC.Services;

/// <summary>
/// Remote mod / modpack search source. Backend not implemented — see <c>Impl.StubModRepository</c>.
/// </summary>
public interface IModRepository
{
    Task<IReadOnlyList<ModInfo>> SearchAsync(string query, CancellationToken ct = default);
}
