namespace nLogViewer.Infrastructure.Helpers.FileProvider;

interface IFileProvider<T> where T : class
{
        /// <summary>
        /// Загрузка содержимого указанного файла
        /// </summary>
        /// <param name="filePath">Путь к файлу</param>
        /// <returns></returns>
        T Load(string filePath);

        /// <summary>
        /// Сохранение данных в файл
        /// </summary>
        /// <param name="obj">Данные</param>
        /// <param name="filePath">Путь к файлу</param>
        void Save(T obj, string filePath);
}
