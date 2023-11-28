using System;
using System.IO;
using Microsoft.Win32;
using System.Data;
using System.Diagnostics;
using HorizonReports.Plugins;
using HorizonReports;
using HorizonReports.Security;
using Microsoft.Extensions.Logging;
using HorizonReports.DataDictionary;
using HorizonReports.ConnectionManagement;
using System.ComponentModel.Composition;

namespace SamplePlugins
{
    /// <summary>
    /// A sample application plugin.
    /// </summary>
    [ApplicationPlugin("{70379C05-4F39-4F92-8EF5-593ACA50972D}",
        "SampleApplicationPlugin",
        PluginSource.Custom,
        Version = "1.0.0.0",
        ExecutionPriority = 5)]
    public class SampleApplicationPlugin : IApplicationPlugin
    {
        /// <summary>
        /// A reference to the Stonefield Query Application object. It must be marked as [Import]
        /// so it's automatically set to the correct object.
        /// </summary>
        [Import]
        public IHorizonReportsAppService Application { get; set; }

        /// <summary>
        /// Called after the user has logged in. This plugin programmatically
        /// adds fields to a table in the data dictionary at runtime to handle inventory attributes.
        /// </summary>
        /// <returns>
        /// True if the application can start, false if not.
        /// </returns>
        public bool AfterLogin(IUser user)
        {
            
                // Get a logger.
                Application.Logger.InfoFormat("Application plugin: adding attrib fields for all datasources");
                // The SQL statement to retrieve a list of the CustomAttrib fields.
                string select = "select distinct CustomAttrib.AttribDefID, CustomAttribDef.AttribDefDesc" +
                    "from CustomAttrib inner join CustomAttribDef " +
                    "on CustomAttrib.AttribDefID = CustomAttribDef.AttribDefID "

                // Get a reference to the database and the InventoryID fields in the tables.
                Database database = Application.DataDictionary.Databases[0];
                Field elementIDField = Application.DataDictionary.Fields["ELEMENT.ELEMENTID"];
                

            try
            {
                Tenant tenant = null;

                if (user.Tenants != null && user.Tenants.Count > 0)
                {
                    tenant = user.Tenants[0];
                }
                    
                if (tenant != null)
                {
                    var datasource = Application.ConnectionManager.DataSources[user.Tenants[0].DataSource];
                    var tenantName = tenant.Name;
                    // Retrieve a list of the CustomAttrib fields.
                    IConnectionFactory factory = datasource[database];
                    IConnection connection = factory.CreateConnection();
                    DataTable attributes = connection.ExecuteSQLStatement(select, new string[] { }, "", null);

                    bool spaceInTenant = tenantName.Contains(" ");
                    // For each attrib field, create a field object in the data dictionary in the Element, 
                    // table. The field calls the GetCustomAttribValue function.
                    Application.Logger.Debug("Sample application plugin: adding attrib fields for datasource =" + tenantName);
                    foreach (DataRow row in attributes.Rows)
                    {
                        string description = row["AttribDefDesc"].ToString().Trim();
                        string caption = description;
                        string id = row["AttribDefID"].ToString();
                        Application.Logger.Debug(String.Format("Adding attrib field: {0} ({1})", description, id));

                        // Add the field to Element if it isn't already there.
                        string fieldName = String.Empty;
                        if (spaceInTenant)
                        {
                            fieldName = "ELEMENT.[ATTRIB" + id + tenantName + "]";
                        }
                        else
                        {
                            fieldName = "ELEMENT.ATTRIB" + id + tenantName;
                        }

                        CalculatedField field = Application.DataDictionary.Fields[fieldName] as CalculatedField;
                        if (field == null)
                        {
                            field = new CalculatedField(fieldName);
                            Application.DataDictionary.Fields.Add(field);
                            field.DataType = typeof(String);
                            field.ExpressionType = ExpressionTypes.Expression;
                            field.Expression = "GetCustomAttribValue(ELEMENT.ELEMENTID, " + id + ")";
                            field.FieldList.Add(elementIDField);
                            field.Tenant = tenant;
                        }
                        field.Caption = caption;
                        field.Heading = description;
                    }
                }
            }
            catch (Exception ex)
            {
                Application.Logger.Error("An error occurred while processing attribute fields for tenant ", ex);
            }
            return true;
        }

        /// <summary>
        /// Fired when Stonefield Query shuts down. This plugin does nothing in the event.
        /// </summary>
        public void OnShutdown()
        {
        }

        /// <summary>
        /// Called before the user has logged in. We're just returning true in this plugin.
        /// </summary>
        /// <param name="userName">
        /// The name of the user logging in.
        /// </param>
        /// <returns>
        /// True if the application can continue.
        /// </returns>
        public bool BeforeLogin(string userName)
        {
            return true;
        }

        /// <summary>
        /// Fired after the application object setup tasks are done. We're just returning true in this plugin.
        /// </summary>
        /// <returns>
        /// True if the application can start, false if not.
        /// </returns>
        public bool AfterSetup()
        {
            return true;
        }

        /// <summary>
        /// Called when a support ticket should be created because of an exception. We're not
        /// doing anything in this plugin.
        /// </summary>
        /// <param name="exception">
        /// The exception that occurred.
        /// </param>
        /// <param name="attachments">
        /// A list of attachments for the ticket.
        /// </param>
        /// <param name="message">
        /// The message for the ticket.
        /// </param>
        /// <returns>
        /// Anything the plugin code wants to return to the caller.
        /// </returns>
        public object CreateSupportTicket(Exception exception, List<string> attachments, string message)
        {
            return null;
        }
    }
}
