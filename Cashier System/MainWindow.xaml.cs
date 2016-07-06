using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Data.SQLite;
using System.IO;

namespace Cashier_System
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<string> _items = new List<string>();
        List<List<string>> _transactions = new List<List<string>>();

        public void CreateTable()
        {
            SQLiteConnection m_dbConnection = new SQLiteConnection("Data Source=C:\\IDDBShared\\IDDatabase.sqlite;Version=3;");
            m_dbConnection.Open();
            string sql = "create table Transactions (TransactionID varchar(20), DatePurchased varchar(20), IDNumber  varchar(20), ItemPurchased  varchar(20), Cost varchar(20), Quantity varchar(20))";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            command.ExecuteNonQuery();
        }

        public bool CheckFirstTime()
        {
            if (Properties.Settings.Default.FirstUse)
            {
                Properties.Settings.Default.FirstUse = false;
                Properties.Settings.Default.Save();

                string pathString = @"C:\IDDBShared";
                if (!(Directory.Exists(pathString)))
                {
                    System.IO.Directory.CreateDirectory(pathString);
                    SQLiteConnection.CreateFile("C:\\IDDBShared\\IDDatabase.sqlite");
                }

                CreateTable();
                return true;
            }
            else
            {
                return false;
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            CheckFirstTime();
        }

        private void addNewItem(string DatePurchased, string CustID, string ItemPurchased, string StockID, string Cost, string Quantity)
        {
            List<string> newTransaction = new List<string>();
            newTransaction.Add(DatePurchased);
            newTransaction.Add(CustID);
            newTransaction.Add(ItemPurchased);
            newTransaction.Add(StockID);
            newTransaction.Add(Cost);
            newTransaction.Add(Quantity);

            _transactions.Add(newTransaction);
        }

        private void BtnAddItem_Click(object sender, RoutedEventArgs e)
        {
            if (!(string.IsNullOrEmpty(TxtStockID.Text)) && !(string.IsNullOrEmpty(TxtQuantity.Text)))
            {
                string stockIDToFind = TxtStockID.Text;

                SQLiteConnection m_dbConnection = new SQLiteConnection("Data Source=C:\\IDDBShared\\IDDatabase.sqlite;Version=3;");
                m_dbConnection.Open();
                string sql = "select * from ShopStock";
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                SQLiteDataReader reader = command.ExecuteReader();

                bool stockDNE = true;
                while (reader.Read())
                {
                    string currentID = Convert.ToString(reader["SKUNumber"]);
                    if (currentID == stockIDToFind)
                    {
                        DateTime thisDay = DateTime.Today;
                        string date = thisDay.ToString("d");
                        string costForOne = Convert.ToString(reader["costAfterMarkup"]);
                        string itemName = Convert.ToString(reader["itemName"]);
                        double total = Convert.ToDouble(TxtQuantity.Text) * Convert.ToDouble(costForOne);
                        string custID = Convert.ToString(lblCurrentCustomerID.Content);
                        int quantity = Convert.ToInt16(TxtQuantity.Text);
                        addNewItem(date, custID, itemName, currentID, Convert.ToString(total), TxtQuantity.Text);
                        string transaction = itemName + "                                                 " + "$" + Convert.ToString(total);
                        _items.Add(transaction);
                        ItemsList.ItemsSource = null;
                        ItemsList.ItemsSource = _items;
                        stockDNE = false;
                        BtnCompletePurchase.IsEnabled = true;
                        TxtQuantity.Text = "";
                        TxtStockID.Text = "";

                        int totalItemsSold = Convert.ToInt16(LblTotalItemsSold.Content);
                        double transactionTotal = Convert.ToDouble(lblTransactionTotal.Content);
                        LblTotalItemsSold.Content = Convert.ToString(quantity + totalItemsSold);
                        lblTransactionTotal.Content = Convert.ToString(transactionTotal + total);
                    }
                }

                if (stockDNE == true)
                {
                    MessageBox.Show("Stock ID does not exist.");
                }
            }
            else
            {
                MessageBox.Show("Please Input a Stock ID.");
            }
        }

        private void BtnSearchCustomer_Click(object sender, RoutedEventArgs e)
        {
            if (!(string.IsNullOrEmpty(TxtCustomerID.Text)))
            {
                string userIDToFind = TxtCustomerID.Text;

                SQLiteConnection m_dbConnection = new SQLiteConnection("Data Source=C:\\IDDBShared\\IDDatabase.sqlite;Version=3;");
                m_dbConnection.Open();
                string sql = "select * from Users";
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                SQLiteDataReader reader = command.ExecuteReader();

                bool userDNE = true;
                while (reader.Read())
                {
                    string currentID = Convert.ToString(reader["IDNumber"]);
                    if (currentID == userIDToFind)
                    {
                        LblBalanceBeforeTransaction.Content = Convert.ToString(reader["currentBalance"]);
                        userDNE = false;
                        BtnAddItem.IsEnabled = true;
                        BtnCancelPurchase.IsEnabled = true;
                        lblCurrentCustomerID.Content = userIDToFind;
                    }
                }

                if (userDNE == true)
                {
                    MessageBox.Show("User does not exist.");
                }
           //     else
      //          {
//
      //          }
            }
            else
            {
                MessageBox.Show("Please Input a User ID.");
            }
        }

        private void getHighestTransactionNumber()
        {
            // For some reason, if this method returns anything, the program freezes...
            string ID;

            SQLiteConnection m_dbConnection = new SQLiteConnection("Data Source=C:\\IDDBShared\\IDDatabase.sqlite;Version=3;");
            m_dbConnection.Open();
            string sql = "select Max(transactionNumber) from Balances";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (reader.GetValue(0) != DBNull.Value)
                {
                    ID = (string)reader.GetValue(0);
                    transID.Content = ID;
                }
                else
                {
                    transID.Content = "1";
                }
           }
        }

        private void BtnCompletePurchase_Click(object sender, RoutedEventArgs e)
        {
            double customerBalanceAfterTransaction = Convert.ToDouble(LblBalanceBeforeTransaction.Content) - Convert.ToDouble(lblTransactionTotal.Content);

            if (customerBalanceAfterTransaction >= 0)
            {
                SQLiteConnection m_dbConnection = new SQLiteConnection("Data Source=C:\\IDDBShared\\IDDatabase.sqlite;Version=3;");
                m_dbConnection.Open();

                getHighestTransactionNumber();
                int IDNumber = Convert.ToInt16(transID.Content);
                string DatePurchased;
                string CustID;
                string ItemPurchased;
                string Cost;
                string Quantity;

                foreach (List<string> transaction in _transactions)
                {
                    IDNumber += 1;
                    string ID = Convert.ToString(IDNumber);
                    DatePurchased = transaction[0];
                    CustID = transaction[1];
                    ItemPurchased = transaction[2];
                    Cost = transaction[4];
                    Quantity = transaction[5];
                    string StockID = transaction[3];
                    string sql = "insert into Transactions (TransactionID, DatePurchased, IDNumber, ItemPurchased, Cost, Quantity) values ('" + ID + "','" + DatePurchased + "'," + CustID + ",'" + ItemPurchased + "','" + Cost + "','" + Quantity + "')";
                    SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                    command.ExecuteNonQuery();

                    string searchSql = "select * from ShopStock";
                    SQLiteCommand searchCommand = new SQLiteCommand(searchSql, m_dbConnection);
                    SQLiteDataReader reader = searchCommand.ExecuteReader();

                    while (reader.Read())
                    {
                        string currentID = Convert.ToString(reader["SKUNumber"]);
                        if (currentID == StockID)
                        {
                            int QuantityInStock = Convert.ToInt16(reader["NumberOfStock"]);
                            QuantityInStock = QuantityInStock - Convert.ToInt16(Quantity);

                            string stockSql = "UPDATE SHOPSTOCK SET NumberOfStock = '" + QuantityInStock + "' WHERE SKUNumber = " + StockID;
                            SQLiteCommand stockCommand = new SQLiteCommand(stockSql, m_dbConnection);
                            stockCommand.ExecuteNonQuery();
                        }
                    }
                }

                string userSql = "UPDATE USERS SET currentBalance = '" + Convert.ToString(customerBalanceAfterTransaction) + "' WHERE IDNumber = " + TxtCustomerID.Text;
                SQLiteCommand userCommand = new SQLiteCommand(userSql, m_dbConnection);
                SQLiteDataReader userReader = userCommand.ExecuteReader();

                TxtCustomerID.Text = "";
                ItemsList.ItemsSource = null;
                lblCurrentCustomerID.Content = "";
                LblTotalItemsSold.Content = "0";
                lblTransactionTotal.Content = "0.00";
                LblBalanceBeforeTransaction.Content = "0.00";
                LblBalanceAfterTransaction.Content = "0.00";
                BtnCompletePurchase.IsEnabled = false;
                BtnCancelPurchase.IsEnabled = false;
                TxtQuantity.Text = "";
                TxtStockID.Text = "";
                _items.Clear();
                _transactions.Clear();
                MessageBox.Show("Transaction Completed.");
            }
            else
            {
                MessageBox.Show("Transaction total is higher than customer balance.");
            }
        }

        public void StockID_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox TxtStockID = (TextBox)sender;
            TxtStockID.Text = string.Empty;
            TxtStockID.GotFocus -= StockID_GotFocus;
        }

        public void Quantity_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox TxtQuantity = (TextBox)sender;
            TxtQuantity.Text = string.Empty;
            TxtQuantity.GotFocus -= Quantity_GotFocus;
        }

        private void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            if (ItemsList.SelectedIndex == -1)
            {
                MessageBox.Show("Please select item to delete.");
            }
            else
            {
                int index = ItemsList.SelectedIndex;
                _transactions.RemoveAt(index);
                _items.RemoveAt(index);
                ItemsList.ItemsSource = null;
                ItemsList.ItemsSource = _items;
                TxtQuantity.Text = "";
                TxtStockID.Text = "";
            }
        }

        private void BtnCancelPurchase_Click(object sender, RoutedEventArgs e)
        {
            TxtCustomerID.Text = "";
            ItemsList.ItemsSource = null;
            lblCurrentCustomerID.Content = "";
            LblTotalItemsSold.Content = "0";
            lblTransactionTotal.Content = "0.00";
            LblBalanceBeforeTransaction.Content = "0.00";
            LblBalanceAfterTransaction.Content = "0.00";
            BtnCompletePurchase.IsEnabled = false;
            BtnCancelPurchase.IsEnabled = false;
            TxtQuantity.Text = "";
            TxtStockID.Text = "";
            _items.Clear();
            _transactions.Clear();
            MessageBox.Show("Transaction Cancelled.");
        }
    }
}
