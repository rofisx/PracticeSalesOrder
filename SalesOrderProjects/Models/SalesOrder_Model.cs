using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SalesOrderProjects.Models
{
    public class SalesOrder_Model
    {
        public string TrsSO { get; set; }
        public string OrderDate { get; set; }
        public string Customer { get; set; }
    }
    public class Customer_Model
    {
        public string CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string Address { get; set; }
    }

    public class ItemDetail
    {
        public string itemName { get; set; }
        public decimal qty { get; set; }
        public decimal price { get; set; }
    }

    public class EditSalesOrder
    {
        public string Trs_SO { get; set; }
        public string OrderDate { get; set; }
        public string CustomerId { get; set; }
        public string Address { get; set; }
        public List<Customer_Model> ListCustomer{ get; set; }        
        public string Addrress { get; set; }
        public List<ItemDetail> ListItemDetail { get; set; }
    }
}