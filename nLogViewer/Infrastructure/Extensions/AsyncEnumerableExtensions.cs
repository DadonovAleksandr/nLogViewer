using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace nLogViewer.Infrastructure.Extensions;

/// <summary>
/// Расширения для работы с асинхронными перечислениями
/// </summary>
internal static class AsyncEnumerableExtensions
{
    /// <summary>
    /// Преобразует обычное перечисление в асинхронное
    /// </summary>
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> source, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var item in source)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return item;
            await Task.Yield(); // Даём возможность другим задачам выполниться
        }
    }
}