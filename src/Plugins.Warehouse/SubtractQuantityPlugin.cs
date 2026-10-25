using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;

namespace Plugins.Warehouse
{
    public class SubtractQuantityPlugin : PluginBase
    {
        public SubtractQuantityPlugin(string unsecureConfiguration, string secureConfiguration)
            : base(typeof(SubtractQuantityPlugin))
        {
        }

        protected override void ExecuteDataversePlugin(ILocalPluginContext localPluginContext)
        {
            if (localPluginContext == null)
            {
                throw new ArgumentNullException(nameof(localPluginContext));
            }

            var context = localPluginContext.PluginExecutionContext;
            if (!context.InputParameters.Contains("Target") || !(context.InputParameters["Target"] is Entity target) ||
                target.LogicalName != "lab_warehousetransaction")
            {
                return;
            }

            if (!target.Contains("lab_quantity") || !target.Contains("lab_itemid") || !target.Contains("lab_transactiontype"))
            {
                return;
            }

            var transactionType = target.GetAttributeValue<OptionSetValue>("lab_transactiontype");
            if (transactionType == null)
            {
                return;
            }

            var quantity = target.GetAttributeValue<int>("lab_quantity");
            var itemReference = target.GetAttributeValue<EntityReference>("lab_itemid");
            var service = localPluginContext.OrgSvcFactory.CreateOrganizationService(context.UserId);
            var item = service.Retrieve("lab_warehouseitem", itemReference.Id, new ColumnSet("lab_availablequantity"));
            var available = item.GetAttributeValue<int>("lab_availablequantity");

            int updatedQuantity;
            if (transactionType.Value == 100000000)
            {
                updatedQuantity = available + quantity;
            }
            else if (transactionType.Value == 100000001)
            {
                updatedQuantity = available - quantity;
            }
            else
            {
                return;
            }

            var itemUpdate = new Entity("lab_warehouseitem", itemReference.Id)
            {
                ["lab_availablequantity"] = updatedQuantity
            };
            service.Update(itemUpdate);
        }
    }
}