namespace Docflow.Impl
{
    using Docflow.Configurations;
    using Docflow.DAL.EntityContext;
    using Docflow.Interface;
    using Docflow.Logging;
    using System;
    using System.Configuration;
    using System.Data.SqlClient;
    using System.IO;
    using System.Linq;

    public class AppEnvironmentChecker : IAppEnvironmentChecker
    {
        /// <summary>
        /// Флаг готовности приложения к работе
        /// </summary>
        public bool AppReadyToWork { get; set; }

        /// <summary>
        /// Проверяем единожды при старте приложения
        /// </summary>
        public void CheckSettings()
        {
            this.AppReadyToWork = true;

            try
            {
                var sqlConnection = new SqlConnection(ConfigurationManager.ConnectionStrings["DBConnection"].ConnectionString);
                sqlConnection.Open();
                sqlConnection.Close();
            }
            catch (Exception e)
            {
                this.AppReadyToWork = false;
                AppLogging.Logger.Fatal(e, "Проверьте подключение DBConnection в WebConfig");
            }


            try
            {
                var sqlConnection = new SqlConnection(ConfigurationManager.ConnectionStrings["FOConnection"].ConnectionString);
                sqlConnection.Open();
                sqlConnection.Close();
            }
            catch (Exception e)
            {
                this.AppReadyToWork = false;
                AppLogging.Logger.Fatal(e, "Проверьте подключение FOConnection в WebConfig");
            }

            try
            {
                var documentContext = new DocumentContext();
                documentContext.Database.Exists();
                var fUt = documentContext.UploadEntityTable.FirstOrDefault();
                var fUtp = documentContext.UploadProgressTable.FirstOrDefault();
                var fSc = documentContext.ScanPaths.FirstOrDefault();

            }
            catch (Exception e)
            {
                this.AppReadyToWork = false;
                AppLogging.Logger.Fatal(e, "Проверьте подключение FOConnection в WebConfig");
            }

            try
            {
                // проверка доступов до дирректорий выгрузки
                if (UserPaths.GetUserPaths().Count == 0)
                {
                    throw  new Exception("Не указаны пользовательские директории для выгрузки!");
                }
                
                foreach (UserPathsElement userPathsElement in UserPaths.GetUserPaths())
                {
                    if (!string.IsNullOrEmpty(userPathsElement.Path))
                    {
                        if (!Directory.Exists(userPathsElement.Path))
                        {
                            throw new Exception("Обнаружены некорректные параметры директорий выгрузки!");
                        }
                    }
                    else
                    {
                        throw new Exception("Обнаружены пустые параметры директорий выгрузки!");
                    }
                }
            }
            catch (Exception e)
            {
                this.AppReadyToWork = false;
                AppLogging.Logger.Fatal(e, "Проверьте секцию userConfigs в WebConfig");
            }
        }
    }
}