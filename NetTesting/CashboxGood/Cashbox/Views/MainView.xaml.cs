﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using Cashbox.DataAccess;
using Cashbox.Models;
using Cashbox.Services;
using Cashbox.ViewModels;

namespace Cashbox.Views
{
    /// <summary>
    /// Interaction logic for MainView.xaml
    /// </summary>
    public partial class MainView
    {
        public MainView()
        {
            InitializeComponent();

            // It would be much better to use IoC container here.
            var unitOfWorkFactory = new UnitOfWorkFactory();
            var service = new DataLoadingService(unitOfWorkFactory);

            DataContext = new MainViewModel(service);
        }

        private void UxProducts_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UxAccounts.SelectedItem == null || UxProducts.SelectedItems == null)
                return;

            UxError.Text = string.Empty;

            using (var context = new CashboxDbContext())
            {
                var accountId = ((Account)UxAccounts.SelectedItem).Id;

                var total = calculateSelectedProductsTotal();
                var discount = calculateDiscount(context, accountId, total);
                var totalAfterDiscount = applyDiscount(total, discount);

                UxTotal.Text = total.ToString();
                UxDiscount.Text = Math.Round(discount * 100, 0) + "%";
                UxTotalAfterDiscount.Text = totalAfterDiscount.ToString();
            }
        }

        private void UxPurchase_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            using (var context = new CashboxDbContext())
            {
                var account = (Account)UxAccounts.SelectedItem;
                context.Accounts.Attach(account);

                var total = calculateSelectedProductsTotal();
                var discount = calculateDiscount(context, account.Id, total);
                var totalAfterDiscount = applyDiscount(total, discount);

                if (totalAfterDiscount <= account.Balance)
                {
                    context.Orders.Add(
                        new Order
                        {
                            Account = account,
                            Date = DateTime.Now,
                            Total = total
                        });

                    account.Balance -= total;

                    foreach (var product in getSelectedProducts())
                    {
                        context.Products.Attach(product);
                        product.Amount--;
                    }

                    context.SaveChanges();
                }
                else
                {
                    UxError.Text = "Not enough money!";
                }
            }
        }

        private IEnumerable<Product> getSelectedProducts()
        {
            return UxProducts.SelectedItems.Cast<Product>();
        }

        private decimal calculateSelectedProductsTotal()
        {
            return getSelectedProducts().Sum(x => x.Price);
        }

        private decimal calculateDiscount(CashboxDbContext context, int accountId, decimal selectedPoductsTotal)
        {
            var discount = 0.0m;

            // If account has orders with sum total >= 500 then give 10% discount.
            var sum = context.Orders.Where(x => x.AccountId == accountId).Sum(x => (decimal?)x.Total) ?? 0;
            if (sum >= 500)
            {
                discount += 0.1m;
            }

            // If the new order has total >= 200 then give an additional 5% discount.
            if (selectedPoductsTotal >= 200)
            {
                discount += 0.05m;
            }

            return discount;
        }

        private decimal applyDiscount(decimal total, decimal discount)
        {
            return Math.Round(total * (1 - discount), 2);
        }
    }
}