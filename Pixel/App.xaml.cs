using System;

using Pixel.Services;

using Windows.ApplicationModel.Activation;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Core;
using Windows.UI.Xaml.Navigation;
using Windows.System;
using Pixel.Views;

namespace Pixel
{

    public sealed partial class App : Application
    {


        private Lazy<ActivationService> _activationService;

        private ActivationService ActivationService
        {
            get { return _activationService.Value; }
        }



        private void App_BackRequested(object sender, BackRequestedEventArgs e)
        {
            e.Handled = On_BackRequested();
        }

        private void On_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            bool isXButton1Pressed =
                e.GetCurrentPoint(sender as UIElement).Properties.PointerUpdateKind == PointerUpdateKind.XButton1Pressed;

            if (isXButton1Pressed)
            {
                e.Handled = On_BackRequested();
            }
        }

        private bool On_BackRequested()
        {
            Frame rootFrame = Window.Current.Content as Frame;
            if (rootFrame.CanGoBack)
            {
                rootFrame.GoBack();
                return true;
            }
            return false;
        }
        public App()
        {


            InitializeComponent();
            UnhandledException += OnAppUnhandledException;

            // Deferred execution until used. Check https://docs.microsoft.com/dotnet/api/system.lazy-1 for further info on Lazy<T> class.
            _activationService = new Lazy<ActivationService>(CreateActivationService);




        }

  

        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            if (!args.PrelaunchActivated)
            {
                await ActivationService.ActivateAsync(args);
            }

            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();


                if (args.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                rootFrame.Navigate(typeof(MainPage), args.Arguments);
            }
            // Ensure the current window is active
            Window.Current.Activate();
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                 rootFrame.CanGoBack ?
                 AppViewBackButtonVisibility.Visible :
                 AppViewBackButtonVisibility.Collapsed;
            SystemNavigationManager.GetForCurrentView().BackRequested += App_BackRequested;

            if (rootFrame != null)
            {
                rootFrame.PointerPressed += On_PointerPressed;
                KeyboardAccelerator GoBack = new KeyboardAccelerator();
                GoBack.Key = VirtualKey.GoBack;

                GoBack.Invoked += BackInvoked;
                KeyboardAccelerator AltLeft = new KeyboardAccelerator();
                AltLeft.Key = VirtualKey.Left;
                AltLeft.Invoked += BackInvoked;

                rootFrame.KeyboardAccelerators.Add(GoBack);
                rootFrame.KeyboardAccelerators.Add(AltLeft);
                // ALT routes here
                AltLeft.Modifiers = VirtualKeyModifiers.Menu;
            }
        }
   
        private void BackInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            On_BackRequested();
            args.Handled = true;
        }
        protected override async void OnActivated(IActivatedEventArgs args)
        {
            await ActivationService.ActivateAsync(args);
        }

        private void OnAppUnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            // TODO WTS: Please log and handle the exception as appropriate to your scenario
            // For more info see https://docs.microsoft.com/uwp/api/windows.ui.xaml.application.unhandledexception
        }

        private ActivationService CreateActivationService()
        {
            return new ActivationService(this, typeof(Views.MainPage));
        }

    }
}
