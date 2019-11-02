﻿using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using LightBulb.Internal;
using LightBulb.Services;
using LightBulb.ViewModels;
using LightBulb.ViewModels.Components;
using LightBulb.ViewModels.Dialogs;
using LightBulb.ViewModels.Framework;
using Stylet;
using StyletIoC;
using MessageBoxViewModel = Stylet.MessageBoxViewModel;

namespace LightBulb
{
    public class Bootstrapper : Bootstrapper<RootViewModel>
    {
        // ReSharper disable once NotAccessedField.Local (need to keep reference)
        private static Mutex _identityMutex;

        public override void Start(string[] args)
        {
            // Ensure this is the only running instance, otherwise - exit
            _identityMutex = new Mutex(true, "LightBulb_Identity", out var isOnlyRunningInstance);
            if (!isOnlyRunningInstance)
            {
                Environment.Exit(0);
                return;
            }

            base.Start(args);
        }

        protected override void ConfigureIoC(IStyletIoCBuilder builder)
        {
            // We don't call base method because that autobinds everything in transient scope.
            // We need to bind our dependencies as singletons due to extensive event routing.

            // Bind services
            builder.Bind<CalculationService>().ToSelf().InSingletonScope();
            builder.Bind<LocationService>().ToSelf().InSingletonScope();
            builder.Bind<SettingsService>().ToSelf().InSingletonScope();
            builder.Bind<SystemService>().ToSelf().InSingletonScope();
            builder.Bind<UpdateService>().ToSelf().InSingletonScope();

            // Bind view model layer services
            builder.Bind<DialogManager>().ToSelf().InSingletonScope();
            builder.Bind<IViewModelFactory>().ToAbstractFactory();

            // Bind view models
            builder.Bind<RootViewModel>().ToSelf().InSingletonScope();
            builder.Bind<MessageBoxViewModel>().ToSelf().InSingletonScope();
            builder.Bind<SettingsViewModel>().ToSelf().InSingletonScope();
            builder.Bind<ISettingsTabViewModel>().ToAllImplementations().InSingletonScope();
        }

        protected override void Launch()
        {
            // Finalize pending updates (and restart) before launching the app
            var updateService = (UpdateService) GetInstance(typeof(UpdateService));
            updateService.FinalizePendingUpdates();

            base.Launch();
        }

        protected override void OnUnhandledException(DispatcherUnhandledExceptionEventArgs e)
        {
            base.OnUnhandledException(e);

            if (!EnvironmentEx.IsDebug())
                MessageBox.Show(e.Exception.ToString(), "Error occured", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}