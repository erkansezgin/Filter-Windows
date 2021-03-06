/*
* Copyright © 2017-2018 Cloudveil Technology Inc.
* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

﻿using CloudVeilGUI.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;

namespace CloudVeilGUI.ViewModels
{
    public class BlockedPagesViewModel
    {
        //public event PropertyChangedEventHandler PropertyChanged;

        private ObservableCollection<BlockedPageEntry> blockedPages;
        public ObservableCollection<BlockedPageEntry> BlockedPages
        {
            get
            {
                return blockedPages;
            }

            set
            {
                blockedPages = value;
                //PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BlockedPages)));
            }
        }

        public BlockedPagesViewModel(BlockedPagesModel model)
        {
            BlockedPages = new ObservableCollection<BlockedPageEntry>(model.BlockedPages);

            model.BlockedPages.CollectionChanged += BlockedPages_CollectionChanged;
        }

        private void BlockedPages_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch(e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems != null)
                    {
                        foreach (var item in e.NewItems)
                        {
                            BlockedPages.Add(item as BlockedPageEntry);
                        }
                    }

                    break;

                case NotifyCollectionChangedAction.Remove:
                    if(e.OldItems != null)
                    {
                        foreach(var item in e.OldItems)
                        {
                            BlockedPages.Add(item as BlockedPageEntry);
                        }
                    }

                    break;
            }
        }
    }
}
