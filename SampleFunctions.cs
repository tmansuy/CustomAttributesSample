using HorizonReports;
using HorizonReports.ConnectionManagement;
using HorizonReports.DataDictionary;
using Microsoft.Extensions.Logging;
using System.ComponentModel.Composition;
using System.Data;
using System.Linq;
using System.Runtime.Caching;

namespace SampleFunctions
{
    public static class SampleFunctions
    {
        /// <summary>
        /// A cache of records from the custom attribute table. Note that there is one dictionary per
        /// data source, since the custom elements are unique for each data source.
        /// </summary>
        private static ObjectCache _customAttribCache = MemoryCache.Default;
        private static string _cacheKey = "CustomAttributeCache";
		private int _cacheExpiry = 300;

        /// <summary>
        /// A reference to the application object.
        /// </summary>
        [Import]
        public static IHorizonReportsAppService Application { get; set; }

        /// <summary>
        /// GetAttributeValue is called from the Expression of fields added to the data dictionary
        /// in SampleApplicationPlugin.AfterLogin.
        /// </summary>
        /// <param name="elementID">
        /// The ID of the current element.
        /// </param>
        /// <param name="attributeID">
        /// The element attribute to get the value for.
        /// </param>
        /// <returns>
        /// The attribute value if it's found or blank if not.
        /// </returns>
        public static string GetAttributeValue(int elementID, int attributeID)
        {
            // Create a key by combining the element and attribute IDs.
            string key = elementID.ToString().PadLeft(20) + attributeID.ToString().PadLeft(20);

            // If we don't already have the lookup data for the current data source, retrieve
            // it and add it to the cache.
            IDataSource datasource = Application.ConnectionManager.DataSources.CurrentDatasource;
            Dictionary<string, string> data = new Dictionary<string, string>();
            string lookupKey = _cacheKey + datasource.Name;
            if (!_customAttribCache.Contains(lookupKey))
            {
                Application.Logger.Info("GetAttributeValue function: getting lookup data");
                string select = "select distinct ElementID, AttribDefID, AttribValue from CustomAttrib where Active = 1";
                Database database = Application.DataDictionary.Databases[0];
                IConnectionFactory factory = datasource[database];
                IConnection connection = factory.CreateConnection();
                DataTable dt = connection.ExecuteSQLStatement(select, new string[] { }, "", null);
                foreach (DataRow row in dt.Rows)
                {
                    string index = row["ElementID"].ToString().PadLeft(20) + row["AttribDefID"].ToString().PadLeft(20);
                    string value = row["AttribValue"].ToString();
                    if (data.ContainsKey(index))
                    {
                        data[index] = data[index] + "," + value;
                    }
                    else
                    {
                        data.Add(index, value);
                    }
                }
                CacheItemPolicy policy = new CacheItemPolicy();
                policy.SlidingExpiration = TimeSpan.FromSeconds(_cacheExpiry);
                _customAttribCache.Set(lookupKey, data, policy);
                Application.Logger.Info("lookup data retrieved successfully");
            }

            // Look for the specified values and return the found value.
            data = _customAttribCache.Get(lookupKey) as Dictionary<string, string>;
            string result = "";
            if (data.ContainsKey(key))
            {
                result = data[key];
            }
            return result.Trim();
        }

    }
}
