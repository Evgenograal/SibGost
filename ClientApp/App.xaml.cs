using ClientApp.Pages;

namespace ClientApp
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            Routing.RegisterRoute("RegisterPage", typeof(RegisterPage));
            Routing.RegisterRoute("ProfilePage", typeof(ProfilePage));
            Routing.RegisterRoute("EditProfilePage", typeof(EditProfilePage));
            Routing.RegisterRoute("ChangePasswordPage", typeof(ChangePasswordPage));
            Routing.RegisterRoute("RoomBookingPage", typeof(RoomBookingPage));
            Routing.RegisterRoute("NotificationsPage", typeof(NotificationsPage));

            MainPage = new AppShell();
        }
    }
}