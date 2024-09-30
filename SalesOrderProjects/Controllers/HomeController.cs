using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Web.Mvc;
using System.Configuration;
using System.Data.SqlClient;
using SalesOrderProjects.Models;

namespace SalesOrderProjects.Controllers
{
    public class HomeController : Controller
    {
        public SqlConnection con;
        //To Handle connection related activities      
        private void connection()
        {
            string constr = ConfigurationManager.ConnectionStrings["connectionkey"].ToString();
            con = new SqlConnection(constr);

        }
        // GET: Home
        public ActionResult Index()
        {
            DataTable dtTrs = new DataTable();
            List<SalesOrder_Model> list_so = new List<SalesOrder_Model>();
            connection();
            con.Open();
            string selAll = @"SELECT 
                            aa.TrsID,aa.CreatedDate,
                            bb.CustomerName
                            FROM TrsSalesOrder aa
                            INNER JOIN
                            Customers bb
                            ON aa.CustomerId = bb.CustomerId ORDER BY aa.CreatedDate DESC";
            using (SqlDataAdapter da = new SqlDataAdapter(selAll, con))
            {
                da.Fill(dtTrs);
            }

            if (dtTrs.Rows.Count > 0)
            {
                foreach (DataRow row in dtTrs.Rows)
                {
                    list_so.Add(new SalesOrder_Model {
                        TrsSO = row["TrsID"].ToString(),
                        OrderDate = DateTime.Parse(row["CreatedDate"].ToString()).ToString("dd/MM/yyyy"),
                        Customer = row["CustomerName"].ToString()
                    });
                }
            }

            con.Close();
            return View(list_so);
        }

        [HttpGet]
        public JsonResult GetTrsSalesOrder(string Keyword, string OrderDate)
        {
            DataTable dtTrs = new DataTable();
            List<SalesOrder_Model> list_so = new List<SalesOrder_Model>();
            try
            {
                string selByKeyword = @"
                SELECT 
                    aa.TrsID,
                    aa.CreatedDate,
                    bb.CustomerName
                FROM 
                    TrsSalesOrder aa
                INNER JOIN 
                    Customers bb ON aa.CustomerId = bb.CustomerId
                WHERE
                    CAST(aa.CreatedDate AS DATE) = @OrderDate
                    AND
                    bb.CustomerName LIKE '%' + @Keyword + '%' 
                ORDER BY 
                    aa.CreatedDate DESC";

                connection();
                con.Open();

                using (SqlCommand cmd = new SqlCommand(selByKeyword, con))
                {
                    cmd.Parameters.Add("@OrderDate", SqlDbType.Date).Value = DateTime.Parse(OrderDate).Date;
                    cmd.Parameters.Add("@Keyword", SqlDbType.VarChar).Value = Keyword;

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dtTrs);
                    }
                }

                con.Close();
            }
            catch (Exception err)
            {
                return Json(new { success = false, message = err.Message });
            }

            if (dtTrs.Rows.Count > 0)
            {
                foreach (DataRow row in dtTrs.Rows)
                {
                    list_so.Add(new SalesOrder_Model
                    {
                        TrsSO = row["TrsID"].ToString(),
                        OrderDate = DateTime.Parse(row["CreatedDate"].ToString()).ToString("dd/MM/yyyy"),
                        Customer = row["CustomerName"].ToString()
                    });
                }
                return Json(new { success = true, result = list_so }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { success = false, message = "Sales Order Not Found" }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult Add() {
            DataTable dtCus = new DataTable();
            string selCustomer = "SELECT CustomerId,CustomerName,ISNULL(Address,'') Address FROM Customers";            
            List<Customer_Model> list_cs = new List<Customer_Model>();
            connection();
            con.Open();
            using (SqlDataAdapter da = new SqlDataAdapter(selCustomer, con))
            {
                da.Fill(dtCus);
            }
            if(dtCus.Rows.Count > 0)
            {
                foreach (DataRow row in dtCus.Rows) {
                    list_cs.Add(new Customer_Model {
                        CustomerId = row["CustomerId"].ToString(),
                        CustomerName = row["CustomerName"].ToString(),
                        Address = row["Address"].ToString()
                    });
                }
            }
            con.Close();
            return View(list_cs);
        }

        [HttpPost]
        public JsonResult Add(string Trs_SO, string OrderDate, 
            string CustomerId, string Address, List<ItemDetail> itemDetails)
        {
            string selTrsById = "SELECT TrsID FROM TrsSalesOrder WHERE TrsID = @TrsSO";
            connection();
            con.Open();
            using (SqlCommand cmd =  new SqlCommand(selTrsById, con))
            {
                cmd.Parameters.AddWithValue("@TrsSO", Trs_SO);
                bool isExist = (new SqlDataAdapter(cmd)).Fill(new DataTable()) > 0;
                if(isExist)
                    return Json(new { success = false, message = "No. Order is Exist" });
            }
            con.Close();

            decimal totalItem = 0, totalAmount = 0;
            foreach (var item in itemDetails)
            {
                totalItem += item.qty;
                totalAmount += (item.qty * item.price);
            }

            con.Open();

            string insertTrsSO = @"INSERT INTO [dbo].[TrsSalesOrder]
                                ([TrsID],[CustomerId],[TotalItem],[TotalAmount],[CreatedDate],[CreatedBy]) 
                                VALUES (@TrsID, @CustomerId,@TotalItem, @TotalAmount,@CreatedDate,'Admin')";

            string insertTrsSODetail = @"INSERT INTO [dbo].[TrsSalesOrderDetail]
                                        ([TrsID],[ItemName],[Qty],[Price]) VALUES (@TrsID, @ItemName,@Qty,@Price)";
            string updAddressCustomer = "UPDATE Customers SET Address = @Address WHERE CustomerId = @CustomerId";
            
            using (SqlTransaction transaction = con.BeginTransaction()) {
                try
                {
                    using(SqlCommand cmd = new SqlCommand(insertTrsSO, con, transaction))
                    {
                        cmd.Parameters.AddWithValue("@TrsID", Trs_SO);
                        cmd.Parameters.AddWithValue("@CustomerId", CustomerId);
                        cmd.Parameters.AddWithValue("@TotalItem", totalItem);
                        cmd.Parameters.AddWithValue("@TotalAmount", totalAmount);
                        cmd.Parameters.AddWithValue("@CreatedDate", DateTime.Parse(OrderDate).ToString("yyyy/MM/dd")+" "+DateTime.Now.ToString("HH:mm:ss"));
                        cmd.ExecuteNonQuery();
                    }

                    using (SqlCommand cmd = new SqlCommand(updAddressCustomer, con, transaction))
                    {
                        cmd.Parameters.AddWithValue("@CustomerId", CustomerId);
                        cmd.Parameters.AddWithValue("@Address", Address);
                        cmd.ExecuteNonQuery();
                    }

                    using (SqlCommand cmd = new SqlCommand(insertTrsSODetail, con, transaction))
                    {
                        cmd.Parameters.Add("@TrsID", SqlDbType.VarChar);
                        cmd.Parameters.Add("@ItemName", SqlDbType.VarChar); 
                        cmd.Parameters.Add("@Qty", SqlDbType.Decimal);
                        cmd.Parameters.Add("@Price", SqlDbType.Decimal);

                        foreach (var item in itemDetails)
                        {
                            cmd.Parameters["@TrsID"].Value = Trs_SO;
                            cmd.Parameters["@ItemName"].Value = item.itemName;
                            cmd.Parameters["@Qty"].Value = item.qty;
                            cmd.Parameters["@Price"].Value = item.price;
                            cmd.ExecuteNonQuery();
                        }                        
                    }
                    transaction.Commit();
                    return Json(new { success = true, message = "Product added successfully!" });
                }
                catch (Exception err)
                {
                    transaction.Rollback();
                    return Json(new { success = false, message = err.Message.ToString() });
                }
            }           
        }

        public ActionResult Edit(string TrsSO)
        {
            DataTable dtCus = new DataTable();
            DataTable dtDetail = new DataTable();
            string selCustomer = "SELECT CustomerId,CustomerName,ISNULL(Address,'') Address FROM Customers";
            string selDetailBySO = @"SELECT 
                                    aa.TrsID, aa.CustomerId,
                                    CASE
	                                    WHEN aa.UpdatedDate = '3000/01/01' 
	                                    THEN CONVERT(VARCHAR(10), CAST(aa.CreatedDate AS DATETIME), 23)
	                                    ELSE CONVERT(VARCHAR(10), CAST(aa.UpdatedDate AS DATETIME), 23)
                                    END OrderDate,
                                    CASE
	                                    WHEN aa.UpdatedBy IS NULL THEN aa.CreatedBy
	                                    ELSE aa.UpdatedBy
                                    END OrderBy,
                                    bb.ItemName,bb.Qty, bb.Price,ISNULL(cc.[Address],'') Address
                                    FROM TrsSalesOrder aa
                                    INNER JOIN 
                                    TrsSalesOrderDetail bb
                                    ON aa.TrsID = bb.TrsID
                                    INNER JOIN Customers cc
                                    ON aa.CustomerId = cc.CustomerId
                                    WHERE aa.TrsID = @TrsSO";
            EditSalesOrder editSalesOrder = new EditSalesOrder();
            connection();
            con.Open();
            using (SqlDataAdapter da = new SqlDataAdapter(selCustomer, con))
            {
                da.Fill(dtCus);
            }
            using (SqlCommand cmd = new SqlCommand(selDetailBySO, con)) {
                cmd.Parameters.AddWithValue("@TrsSO", TrsSO);

                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dtDetail);
                }
            }            
            con.Close();
            if(dtCus.Rows.Count > 0)
            {
                editSalesOrder.ListCustomer = new List<Customer_Model>();
                foreach (DataRow row in dtCus.Rows)
                {                    
                    editSalesOrder.ListCustomer.Add(new Customer_Model {
                        CustomerId = row["CustomerId"].ToString(),
                        CustomerName = row["CustomerName"].ToString(),
                        Address = row["Address"].ToString()
                    });
                }
            }
            if(dtDetail.Rows.Count > 0)
            {
                editSalesOrder.Trs_SO = dtDetail.Rows[0]["TrsID"].ToString();
                editSalesOrder.OrderDate = dtDetail.Rows[0]["OrderDate"].ToString();
                editSalesOrder.CustomerId = dtDetail.Rows[0]["CustomerId"].ToString();
                editSalesOrder.Address = dtDetail.Rows[0]["Address"].ToString();
                editSalesOrder.ListItemDetail = new List<ItemDetail>();
                foreach (DataRow row in dtDetail.Rows)
                {                    
                    editSalesOrder.ListItemDetail.Add(new ItemDetail {
                        itemName = row["ItemName"].ToString(),
                        qty = decimal.Parse(row["Qty"].ToString()),
                        price = decimal.Parse(row["Price"].ToString())
                    });
                }
            }
            return View(editSalesOrder);
        }

        [HttpPost]
        public JsonResult Edit(string Trs_SO, string OrderDate,
    string CustomerId, string Address, List<ItemDetail> itemDetails)
        {
            string selTrsById = "SELECT TrsID FROM TrsSalesOrder WHERE TrsID = @TrsSO";
            connection();
            con.Open();
            using (SqlCommand cmd = new SqlCommand(selTrsById, con))
            {
                cmd.Parameters.AddWithValue("@TrsSO", Trs_SO);
                bool isExist = (new SqlDataAdapter(cmd)).Fill(new DataTable()) > 0;
                if (!isExist)
                    return Json(new { success = false, message = "No. Order is Not Exist" });
            }
            con.Close();

            decimal totalItem = 0, totalAmount = 0;
            foreach (var item in itemDetails)
            {
                totalItem += item.qty;
                totalAmount += (item.qty * item.price);
            }

            con.Open();

            string updTrsSO = @"UPDATE [dbo].[TrsSalesOrder]
                                SET [CustomerId] = @CustomerId,
                                    [TotalItem] = @TotalItem,[TotalAmount] = @TotalAmount,
                                    [UpdatedDate] = @UpdatedDate,[UpdatedBy] = 'Admin'
                                WHERE [TrsID] = @TrsID";
            string delTrsSODetail = "DELETE FROM [dbo].[TrsSalesOrderDetail] WHERE TrsID = @TrsID";
            string insertTrsSODetail = @"INSERT INTO [dbo].[TrsSalesOrderDetail]
                                        ([TrsID],[ItemName],[Qty],[Price]) VALUES (@TrsID, @ItemName,@Qty,@Price)";
            string updAddressCustomer = @"UPDATE Customers SET Address = @Address,[UpdatedDate] = @UpdatedDate,[UpdatedBy] = 'Admin' 
                                         WHERE CustomerId = @CustomerId";

            using (SqlTransaction transaction = con.BeginTransaction())
            {
                try
                {
                    using (SqlCommand cmd = new SqlCommand(updTrsSO, con, transaction))
                    {
                        cmd.Parameters.AddWithValue("@TrsID", Trs_SO);
                        cmd.Parameters.AddWithValue("@CustomerId", CustomerId);
                        cmd.Parameters.AddWithValue("@TotalItem", totalItem);
                        cmd.Parameters.AddWithValue("@TotalAmount", totalAmount);
                        cmd.Parameters.AddWithValue("@UpdatedDate", DateTime.Parse(OrderDate).ToString("yyyy/MM/dd") + " " + DateTime.Now.ToString("HH:mm:ss"));
                        cmd.ExecuteNonQuery();
                    }

                    using (SqlCommand cmd = new SqlCommand(updAddressCustomer, con, transaction))
                    {
                        cmd.Parameters.AddWithValue("@CustomerId", CustomerId);
                        cmd.Parameters.AddWithValue("@Address", Address);
                        cmd.Parameters.AddWithValue("@UpdatedDate", DateTime.Parse(OrderDate).ToString("yyyy/MM/dd") + " " + DateTime.Now.ToString("HH:mm:ss"));
                        cmd.ExecuteNonQuery();
                    }

                    using (SqlCommand cmd = new SqlCommand(delTrsSODetail, con, transaction))
                    {
                        cmd.Parameters.AddWithValue("@TrsID", Trs_SO);
                        cmd.ExecuteNonQuery();
                    }

                    using (SqlCommand cmd = new SqlCommand(insertTrsSODetail, con, transaction))
                    {
                        cmd.Parameters.Add("@TrsID", SqlDbType.VarChar);
                        cmd.Parameters.Add("@ItemName", SqlDbType.VarChar);
                        cmd.Parameters.Add("@Qty", SqlDbType.Decimal);
                        cmd.Parameters.Add("@Price", SqlDbType.Decimal);

                        foreach (var item in itemDetails)
                        {
                            cmd.Parameters["@TrsID"].Value = Trs_SO;
                            cmd.Parameters["@ItemName"].Value = item.itemName;
                            cmd.Parameters["@Qty"].Value = item.qty;
                            cmd.Parameters["@Price"].Value = item.price;
                            cmd.ExecuteNonQuery();
                        }
                    }
                    transaction.Commit();
                    return Json(new { success = true, message = "Product Edited Successfully!" });
                }
                catch (Exception err)
                {
                    transaction.Rollback();
                    return Json(new { success = false, message = err.Message.ToString() });
                }
            }
        }

        [HttpPost]
        public JsonResult Delete(string TrsSO)
        {           
            connection();
            con.Open();
            string delTrsSO = "DELETE FROM [dbo].[TrsSalesOrder] WHERE TrsID = @TrsID";
            string delTrsSODetail = "DELETE FROM [dbo].[TrsSalesOrderDetail] WHERE TrsID = @TrsID";           

            using (SqlTransaction transaction = con.BeginTransaction())
            {
                try
                {
                    
                    using (SqlCommand cmd = new SqlCommand(delTrsSODetail, con, transaction))
                    {
                        cmd.Parameters.AddWithValue("@TrsID", TrsSO);
                        cmd.ExecuteNonQuery();
                    }
                    using (SqlCommand cmd = new SqlCommand(delTrsSO, con, transaction))
                    {
                        cmd.Parameters.AddWithValue("@TrsID", TrsSO);
                        cmd.ExecuteNonQuery();
                    }
                    transaction.Commit();
                    return Json(new { success = true, message = "Product Deleted Successfully!" });
                }
                catch (Exception err)
                {
                    transaction.Rollback();
                    return Json(new { success = false, message = err.Message.ToString() });
                }
            }
        }
    }
}