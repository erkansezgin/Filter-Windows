/*
* Copyright © 2017-2018 Cloudveil Technology Inc.
* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

﻿using Citadel.Core.Windows.Util;
using CloudVeilGUI.Platform.Common;
using CloudVeilGUI.Platform.Windows;
using Filter.Platform.Common;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace CloudVeilGUI.WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class WPFApp : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            Citadel.Core.Windows.Platform.Init();
            PlatformTypes.Register<IFilterStarter>((arr) => new WindowsFilterStarter());
            PlatformTypes.Register<IGuiServices>((arr) => new WindowsGuiServices());
            PlatformTypes.Register<ITrayIconController>((arr) => new WindowsTrayIconController());

            base.OnStartup(e);
        }

        protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
        {
            
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // TODO: Use to call custom OnExit() in CloudVeilGUI
            base.OnExit(e);
        }
    }
}
