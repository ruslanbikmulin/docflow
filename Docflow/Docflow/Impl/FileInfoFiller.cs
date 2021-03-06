﻿using System.Text.RegularExpressions;

namespace Docflow.Impl
{
    using Docflow.DAL.Models;
    using Docflow.DtoEntities;
    using Docflow.Enums;
    using Docflow.Interface;
    using Docflow.Logging;
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Data;
    using System.Data.SqlClient;
    using System.IO;

    public class FileInfoFiller : IFileInfoFiller
    {
        private const string IncorporationDocumentTypeName = "'Паспорт'";

        private const string BankingServiceSubtypeName = "'Транш'";

        public FileInfoFillerResult FillFileInfo(string contractName)
        {
            var result = new FileInfoFillerResult()
            {
                ScanPaths = new List<ScanPath>(),
                Success = true
            };

            using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["FOConnection"].ConnectionString))
            {
                connection.Open();
                #region Получаем договор
                var contractSql = "SELECT TOP(1) Id, ContactId, CASE WHEN BankingServiceSubTypeId IN (SELECT Id FROM BankingServiceSubtype WHERE Name = "+ BankingServiceSubtypeName + ") THEN 1 ELSE 0 END AS IsTranche FROM Contract WHERE Number=@p";

                var getContractCommand = new SqlCommand(contractSql);
                getContractCommand.Connection = connection;

                

                var numberParam = new SqlParameter("@p", SqlDbType.NVarChar);
                numberParam.Value = contractName;

                getContractCommand.Parameters.Add(numberParam);

                var contractId = Guid.Empty;
                var contactId = Guid.Empty;
                var isTranche = 0;

                SqlTransaction getContracTransaction = connection.BeginTransaction(IsolationLevel.ReadUncommitted);
                getContractCommand.Transaction = getContracTransaction;

                try
                {
                    using (var reader = getContractCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            contractId = Guid.Parse(reader["Id"].ToString());
                            contactId = Guid.Parse(reader["ContactId"].ToString());
                            isTranche = int.Parse(reader["IsTranche"].ToString());
                        }
                    }
                    getContracTransaction.Commit();
                }
                catch (Exception e)
                {

                    AppLogging.Logger.Debug("Ошибка на этапе выполнения команды по получению строк из таблицы Contract." + e.Message + e.StackTrace);
                    getContracTransaction.Rollback();
                }

                if (contractId == Guid.Empty)
                {
                    result.ErrorMessage = "Не найден договор с идентификатором: " + contractName;
                    result.Success = false;

                    return result;
                }
                #endregion

                AppLogging.Logger.Debug("Нашли контракт " + contractId);

                AppLogging.Logger.Debug("Начинаем получать информацию по файлам договора и контакта");


                var contractFilesSql = @"SELECT CNTRF.Name, LFSD.DiskPath+'\ContractFile\'+LOWER(CNTRF.Id) as Path "
                                       + @"FROM ContractFile as CNTRF "
                                       + @"JOIN LocalFileStorageDirectory AS LFSD ON ( Cast(CNTRF.CreatedOn AS DATE) >= LFSD.DateFrom and  Cast(CNTRF.CreatedOn AS DATE) <= LFSD.DateTo) "
                                       + @"WHERE ContractId=@contractId "
                                       + @"UNION ALL "
                                       + @"SELECT cff.Name,LFSD.DiskPath+'\ContactFilingFile\'+LOWER(cff.Id) AS Path "
                                       + @"FROM ContactFilingFile AS cff "
                                       + @"JOIN IncorporateDocumentInContact idc on idc.id = cff.ContactFilingId "
                                       + @"JOIN LocalFileStorageDirectory AS LFSD ON ( Cast(cff.CreatedOn AS DATE) >= LFSD.DateFrom and  Cast(cff.CreatedOn AS DATE) <= LFSD.DateTo ) "
                                       + @"WHERE idc.ContactId = (SELECT ContactId FROM Contract WHERE Id=@contractId) "
                                       + @"AND idc.DocumentTypeId =  (SELECT Id FROM IncorporationDocumentType WHERE Name =" + IncorporationDocumentTypeName + ")";






                #region Получаем инфу по сканам договора и контакта


                //var contractFilesSql = @"SELECT CNTRF.Name, '\\dc1-ltfo-app1\files\ContractFile\'+LOWER(CNTRF.Id) as Path "
                //                     + @"FROM ContractFile as CNTRF "
                //                     + @"WHERE ContractId=@contractId "
                //                     + @"UNION ALL "
                //                     + @"SELECT cff.Name, '\\dc1-ltfo-app1\files\ContactFilingFile\'+LOWER(cff.Id) AS Path "
                //                     + @"FROM ContactFilingFile AS cff "
                //                     + @"JOIN IncorporateDocumentInContact idc on idc.id = cff.ContactFilingId "
                //                     + @"WHERE idc.ContactId = (SELECT ContactId FROM Contract WHERE Id=@contractId) "
                //                     + @"AND idc.DocumentTypeId =  (SELECT Id FROM IncorporationDocumentType WHERE Name =" + IncorporationDocumentTypeName + ")";




                var contractFilesCommand = new SqlCommand(contractFilesSql);

                contractFilesCommand.Connection = connection;

                

                var contractIdParam = new SqlParameter("@contractId", SqlDbType.UniqueIdentifier);
                contractIdParam.Value = contractId;
                contractFilesCommand.Parameters.Add(contractIdParam);

                SqlTransaction getPathTransaction = connection.BeginTransaction(IsolationLevel.ReadUncommitted);
                contractFilesCommand.Transaction = getPathTransaction;

                try
                {
                    using (var reader = contractFilesCommand.ExecuteReader())
                    {
                        AppLogging.Logger.Debug("Исполнили запрос на получение путей");
                        while (reader.Read())
                        {
                            var scanPath = new ScanPath();
                            var filePath = reader["Path"].ToString();


                            AppLogging.Logger.Debug("Исходный путь:" + filePath);

                            if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["ServerSource"]))
                            {
                                Regex rgx = new Regex(@"^[\\]{2}[a-z0-9]{1,5}-[a-z0-9]{1,6}-[a-z0-9]{1,6}");

                                filePath = rgx.Replace(filePath, ConfigurationManager.AppSettings["ServerSource"]);
                            }

                            AppLogging.Logger.Debug("Результирующий путь:" + filePath);

                            AppLogging.Logger.Debug("Нашли путь до файла:" + filePath);

                            try
                            {
                                if (File.Exists(filePath))
                                {
                                    scanPath.FileName = reader["Name"].ToString();
                                    scanPath.FilePath = filePath;
                                    scanPath.ScanStatus = ScanStatus.FilePathAssigned;
                                    AppLogging.Logger.Debug(string.Format("Файл: {0} по договору: {1} найден, его путь: {2}", scanPath.FileName, contractName, filePath));
                                }
                                else
                                {
                                    AppLogging.Logger.Debug(string.Format("Не найден файл: {0} по договору: {1} , его путь: {2}", scanPath.FileName, contractName, filePath));
                                    scanPath.ScanStatus = ScanStatus.FileNotExist;
                                }
                            }
                            catch (Exception e)
                            {
                                AppLogging.Logger.Debug("Ошибка на этапе проверки существования файла по пути. Путь : " + filePath);
                                scanPath.ScanStatus = ScanStatus.Error;
                            }

                            result.ScanPaths.Add(scanPath);
                        }
                    }

                    getPathTransaction.Commit();
                }
                catch (Exception e)
                {
                    result.Success = false;
                    AppLogging.Logger.Debug("Ошибка на этапе выполнения команды по получению строк из таблицы ContractFile. Описание: " + e.Message);
                    getPathTransaction.Rollback();
                }
                #endregion

                #region Пытаемся достать инфу по микрозайму если текущий договор является траншем



                if (isTranche == 1)
                {
                    AppLogging.Logger.Debug("Договор является траншем");

                    // то надо получать связанный договор ДМЛ по контакту
                    var dmlContractSql = "SELECT TOP(1) Id FROM Contract " +
                                         "WHERE StatusId not in (SELECT Id FROM ContractStatus WHERE Name = 'Закрыт' OR Name = 'Аннулирован' OR Name = 'Погашен' OR Name = 'Прощен и погашен') " +
                                         "AND BankingServiceSubTypeId=(SELECT TOP(1) Id FROM BankingServiceSubtype WHERE Name = 'ДМЛ') " +
                                         "AND ContactId =@contactId ";
                    var dmlContractCommand = new SqlCommand(dmlContractSql);
                    dmlContractCommand.Connection = connection;

                    


                    var contactIdParams = new SqlParameter("@contactId", SqlDbType.UniqueIdentifier);
                    contactIdParams.Value = contactId;
                    dmlContractCommand.Parameters.Add(contactIdParams);
                    var dmlContractId = Guid.Empty;

                    SqlTransaction getDmlTransaction = connection.BeginTransaction(IsolationLevel.ReadUncommitted);
                    dmlContractCommand.Transaction = getDmlTransaction;

                    try
                    {
                        using (var reader = dmlContractCommand.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                dmlContractId = Guid.Parse(reader["Id"].ToString());
                            }
                        }

                        getDmlTransaction.Commit();
                    }
                    catch (Exception e)
                    {
                        AppLogging.Logger.Debug("Ошибка на этапе получения ДМЛ договора. Описание: " + e.Message);
                        getDmlTransaction.Rollback();
                    }


                    if (dmlContractId == Guid.Empty)
                    {
                        AppLogging.Logger.Debug("Не удалось найти договор ДМЛ, хотя входяший номер договра был Траншем.");
                        return result;
                    }

                    // если таки нашли то можем работать над этим.
                    // пытаемся получить набор файлов по договору ДМЛ
                    var dmlFilesPathsSql = @"SELECT CNTRF.Name, LFSD.DiskPath+'\ContractFile\'+LOWER(CNTRF.Id) as Path "
                                         + @"FROM ContractFile as CNTRF "
                                         + @"JOIN LocalFileStorageDirectory AS LFSD ON ( Cast(CNTRF.CreatedOn AS DATE) >= LFSD.DateFrom and  Cast(CNTRF.CreatedOn AS DATE) <= LFSD.DateTo ) "
                                         + @"WHERE ContractId=@dmlContractId ";


                    //var dmlFilesPathsSql = @"SELECT CNTRF.Name, '\\dc1-ltfo-app1\files\ContractFile\'+LOWER(CNTRF.Id) as Path "
                    //                       + @"FROM ContractFile as CNTRF "
                    //                       + @"WHERE ContractId=@dmlContractId ";

                    var dmlFilesPathsCommand = new SqlCommand(dmlFilesPathsSql);
                    dmlFilesPathsCommand.Connection = connection;

                    


                    var dmlContractIdParametr = new SqlParameter("@dmlContractId", SqlDbType.UniqueIdentifier);
                    dmlContractIdParametr.Value = dmlContractId;
                    dmlFilesPathsCommand.Parameters.Add(dmlContractIdParametr);

                    SqlTransaction getDmlPathTransaction = connection.BeginTransaction(IsolationLevel.ReadUncommitted);
                    dmlFilesPathsCommand.Transaction = getDmlPathTransaction;

                    AppLogging.Logger.Debug("Пытаемся найти пути по ДМЛ договору");
                    try
                    {
                        using (var reader = dmlFilesPathsCommand.ExecuteReader())
                        {
                            AppLogging.Logger.Debug("Исполнили запрос на получение путей");
                            while (reader.Read())
                            {
                                var scanPath = new ScanPath()
                                {
                                    NeedPathForDml = true
                                };

                                var filePath = reader["Path"].ToString();
                                AppLogging.Logger.Debug("Исходный путь:" + filePath);

                                if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["ServerSource"]))
                                {
                                    Regex rgx = new Regex(@"^[\\]{2}[a-z0-9]{1,5}-[a-z0-9]{1,6}-[a-z0-9]{1,6}");

                                    filePath = rgx.Replace(filePath, ConfigurationManager.AppSettings["ServerSource"]);
                                }

                                AppLogging.Logger.Debug("Результирующий путь:" + filePath);

                                AppLogging.Logger.Debug("Нашли путь до файла:" + filePath);
                                try
                                {
                                    if (File.Exists(filePath))
                                    {
                                        scanPath.FileName = reader["Name"].ToString();
                                        scanPath.FilePath = filePath;
                                        scanPath.ScanStatus = ScanStatus.FilePathAssigned;
                                        AppLogging.Logger.Debug(string.Format("Файл: {0} по ДМЛ договору: {1} найден, его путь: {2}", scanPath.FileName, contractName, filePath));
                                    }
                                    else
                                    {
                                        scanPath.ScanStatus = ScanStatus.FileNotExist;
                                    }
                                }
                                catch (Exception e)
                                {
                                    AppLogging.Logger.Debug("Ошибка на этапе проверки существования файла по пути для ДМЛ договора. Путь : " + filePath);
                                    scanPath.ScanStatus = ScanStatus.Error;
                                }

                                result.ScanPaths.Add(scanPath);
                                
                            }
                        }

                        getDmlPathTransaction.Commit();
                    }
                    catch (Exception e)
                    {
                        result.Success = false;
                        AppLogging.Logger.Debug("Ошибка на этапе получения путей для файлов ДМЛ договора. Описание: " + e.Message);
                        getDmlPathTransaction.Rollback();
                    }
                }
                

                #endregion


                if (result.ScanPaths.Count == 0)
                {
                    result.Success = false;
                }


                connection.Close();
            }
            
            return result;
        }

        public void Dispose()
        {
            
        }
    }
}