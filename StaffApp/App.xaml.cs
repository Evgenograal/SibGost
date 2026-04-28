using Microsoft.Extensions.DependencyInjection;
using StaffApp.Pages;

namespace StaffApp
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            Routing.RegisterRoute("LoginPage", typeof(LoginPage));
            Routing.RegisterRoute("ManageAmenitiesPage", typeof(ManageAmenitiesPage));
            Routing.RegisterRoute("ManageCategoriesPage", typeof (ManageCategoriesPage));
            Routing.RegisterRoute("ManagePricePoliciesPage", typeof(ManagePricePoliciesPage));
            Routing.RegisterRoute("ManageUsersPage", typeof(ManageUsersPage));
            Routing.RegisterRoute("ManageRoomsPage", typeof(ManageRoomsPage));
            Routing.RegisterRoute("ManageBookingsPage", typeof(ManageBookingsPage));
            Routing.RegisterRoute("BookingDetailPage", typeof(BookingDetailPage));
            Routing.RegisterRoute("CreateBookingPage", typeof(CreateBookingPage));

            MainPage = new AppShell();
        }
    }
}