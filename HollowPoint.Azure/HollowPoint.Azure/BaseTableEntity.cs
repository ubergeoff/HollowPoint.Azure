using HollowPoint.Azure.Common;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Data.Services.Common;

namespace HollowPoint.Azure
{
    [DataServiceKey("PartitionKey", "RowKey")]
    public class BaseTableEntity : TableEntity
    {

        private DateTime _dateTime = DateTime.Now;
        private string _title = string.Empty;
        private bool _isDeleted = false;

        public DateTime DateAdded
        {
            get
            {
                return _dateTime;
            }
            set
            {
                _dateTime = value;
            }
        }
        public string Title
        {
            get
            {
                return _title;
            }
            set
            {
                _title = value;
            }
        }

        public bool IsDeleted
        {
            get
            {
                return _isDeleted;
            }
            set
            {
                _isDeleted = value;
            }
        }



        public override string ToString()
        {
            return Title;
        }

       
     


    }


}