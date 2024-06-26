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
using System.Collections.Generic;
using System.Windows;
using SharpPad.WPF.AdvancedMenuService.ContextService;
using SharpPad.WPF.Interactivity.Contexts;

namespace SharpPad.WPF.AdvancedMenuService.RegularMenuService
{
    // TODO: not completed yet. Need to add a context generator property
    public class AdvancedRegularMenu : ContextCapturingMenu, IAdvancedContainer
    {
        private readonly Dictionary<Type, Stack<FrameworkElement>> itemCache;

        public IContextData Context { get; internal set; }

        public AdvancedRegularMenu()
        {
            this.itemCache = new Dictionary<Type, Stack<FrameworkElement>>();
        }

        public bool PushCachedItem(Type entryType, FrameworkElement item) => MenuService.PushCachedItem(this.itemCache, entryType, item);

        public FrameworkElement PopCachedItem(Type entryType) => MenuService.PopCachedItem(this.itemCache, entryType);

        public FrameworkElement CreateChildItem(IContextEntry entry) => MenuService.CreateChildItem(this, entry);
    }
}