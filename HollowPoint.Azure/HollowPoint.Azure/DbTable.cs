using HollowPoint.Azure.Common;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Table.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Caching;
using HollowPoint.Azure.Extensions;


namespace HollowPoint.Azure
{
    public partial class DbTable<T> where T : BaseTableEntity, new()
    {
        private T _value;
        private T Instance
        {
            get
            {
                if (_value == null)
                {
                    _value = new T();
                }

                return _value;
            }
        }

        public DbTable(CloudTableClient tableClient = null)
        {
            _tableClient = tableClient;
        }

    


        private CloudTable _cloudTable;

        //dont dispose this
        private CloudTableClient _tableClient;

        internal void Dispose()
        {           
            _cloudTable = null;
            _value = null;


        }

        private CloudTable Table
        {

            get
            {
                if (_cloudTable == null)
                {
                    _cloudTable = GetTableReference(TypeName);
                }

                return _cloudTable;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="table"></param>
        /// <returns></returns>
        private bool TableExists(string tableName, CloudTable table)
        {
            bool exists = false;
            object obj = null;
            var cache = HttpContext.Current.Cache;

            try
            {
                obj = cache.Get(tableName);
            }
            finally
            {
                if (obj == null)
                {
                    //create the table is it does not exist
                    //               OR
                    //Ensure that the table already exist

                    try
                    {
                        table.CreateIfNotExists();
                    }
                    catch (StorageException storeEX)
                    {
                        if (storeEX.RequestInformation.HttpStatusCode == Int32.Parse(HttpStatusCode.Conflict.ToString()) &&
                            storeEX.RequestInformation.ExtendedErrorInformation.ErrorMessage == TableErrorCodeStrings.TableAlreadyExists)
                        {
                            exists = true;
                        }

                    }
                    cache.Insert(tableName, tableName + " exists", null, DateTime.Now.AddDays(1), Cache.NoSlidingExpiration);
                    exists = true;


                }
                else
                    exists = true;
            }

            return exists;

        }

        private CloudTable GetTableReference(string tableName)
        {
            if (_tableClient == null)
            {
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));

                // Create the table client.
                _tableClient = storageAccount.CreateCloudTableClient();
            }
            //create a refrence to the table
            CloudTable table = _tableClient.GetTableReference(tableName);

            if (TableExists(tableName, table))
            {
                return table;
            }
            else
                throw new Exception("Unable to create Azure table");

        }

        private void CheckForRequiredFields(ITableEntity entity)
        {
            if (string.IsNullOrEmpty(entity.RowKey))
                throw new ArgumentNullException("RowKey");

            if (string.IsNullOrEmpty(entity.PartitionKey))
                throw new ArgumentNullException("PartitionKey");
        }

        public void Add(ITableEntity entity)
        {
            CheckForRequiredFields(entity);

            // Execute the insert operation.
            Table.Execute(TableOperation.Insert(entity));
        }

        public void Delete(ITableEntity entity)
        {
            //CheckForRequiredFields(entity);

            // Execute the insert operation.
            Table.Execute(TableOperation.Delete(entity));
        }

        public void Update(ITableEntity entity)
        {
            CheckForRequiredFields(entity);
            // Execute the insert operation.
            Table.Execute(TableOperation.Replace(entity));
        }

        public async Task<TableResult> AddAsync(ITableEntity entity)
        {
            CheckForRequiredFields(entity);
            // Execute the insert operation.
            return await Table.ExecuteAsync(TableOperation.Insert(entity));
        }

        public async Task<TableResult> UpdateAsync(ITableEntity entity)
        {
            CheckForRequiredFields(entity);
            // Execute the insert operation.
            return await Table.ExecuteAsync(TableOperation.Replace(entity));
        }

        public List<T> Execute(TableQuery<T> query)
        {
            var entities = new List<T>();

            foreach (var item in Table.ExecuteQuery(query))
            {
                entities.Add(item);
            }

            return entities;
        }

        private T GetByRowKey(string rowKey, string partitionKey)
        {
            return Table.CreateQuery<T>()
                .Where(t => t.PartitionKey == partitionKey && t.RowKey == rowKey)
                .FirstOrDefault();

        }


        private T GetByPartitionKey(string partitionKey)
        {
            return Table.CreateQuery<T>()
                .Where(t => t.PartitionKey == partitionKey)
                .FirstOrDefault();

        }

        public T Get(string partitionKey, string rowKey = null)
        {
            if (rowKey == null)
            {
                return GetByPartitionKey(partitionKey);
            }
            else
            {
                return GetByRowKey(rowKey, partitionKey);
            }
        }

      

        public List<T> GetAllByPartitionKey(string key)
        {

            var query = Table.CreateQuery<T>()
                .Where(t => t.PartitionKey == key);

            return query.ToList();
        }

        public async Task<TableResult> FindAsync(string partitionKey, string rowKey)
        {
            TableOperation retrieveOperation = TableOperation.Retrieve<T>(partitionKey, rowKey);
            return await Table.ExecuteAsync(retrieveOperation);
        }

        public TableQuery<T> Query()
        {
            return Table.CreateQuery<T>();
        }



        string _typeName;
        private string TypeName
        {
            get
            {
                if (string.IsNullOrEmpty(_typeName))
                {
                    string type = this.GetType().ToString();
                    var shortTypeArray = Helpers.QuickSplit(type, ".][");
                    _typeName = shortTypeArray[shortTypeArray.GetUpperBound(0)];
                }

                return _typeName;
            }
        }

      

    }
}