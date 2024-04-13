//
// Copyright (c) 2023-2024 REghZy
//
// This file is part of SharpPad.
//
// SharpPad is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either
// version 3.0 of the License, or (at your option) any later version.
//
// SharpPad is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with SharpPad. If not, see <https://www.gnu.org/licenses/>.
//

using System;
using Avalonia.Threading;

namespace SharpPad.Avalonia;

/// <summary>
/// An application instance for SharpPad
/// </summary>
public class ApplicationCore
{
    private readonly ServiceManager serviceManager;

    public IServiceManager Services => this.serviceManager;

    public static ApplicationCore Instance { get; } = new ApplicationCore();

    /// <summary>
    /// Gets the current version of the application. This value does not change during runtime.
    /// <para>The <see cref="Version.Major"/> property is used to represent a rewrite of the application (for next update)</para>
    /// <para>The <see cref="Version.Minor"/> property is used to represent a large change (for next update)</para>
    /// <para>The <see cref="Version.Build"/> property is used to represent any change to the code (for next update)</para>
    /// <para>
    /// 'for next update' meaning the number is incremented when there's a push to the github, as this is
    /// easiest to track. Many different changes can count as one update
    /// </para>
    /// </summary>
    public Version CurrentVersion { get; } = new Version(1, 0, 0, 0);

    /// <summary>
    /// Gets the current build version of this application. This accesses <see cref="CurrentVersion"/>, and changes whenever a new change is made to the application (regardless of how small)
    /// </summary>
    public int CurrentBuild => this.CurrentVersion.Build;

    public Dispatcher Dispatcher { get; }

    private ApplicationCore()
    {
        this.Dispatcher = Dispatcher.UIThread;
        this.serviceManager = new ServiceManager();
    }
}