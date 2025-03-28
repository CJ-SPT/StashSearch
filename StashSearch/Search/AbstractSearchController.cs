﻿using StashSearch.Utils;
using System;
using System.Collections.Generic;
using EFT.UI.DragAndDrop;

namespace StashSearch.Search;

internal abstract class AbstractSearchController
{
    public bool IsSearchedState;
    public string CurrentSearchString;
    public StashGridClass SearchedGrid;
    public string ParentGridId;
    public GridView GridView;
    public GridViewOwner SearchControllerOwner = GridViewOwner.None;
    
    /// <summary>
    /// This is a list of items we want to restore once we're done with our searched items
    /// </summary>
    protected List<ContainerItem> itemsToRestore = new();

    /// <summary>
    /// Restore to presearched state
    /// </summary>
    /// <param name="gridToRestore"></param>
    /// <exception cref="Exception"></exception>
    public abstract void RestoreHiddenItems(StashGridClass gridToRestore, GridView gridView);
}
