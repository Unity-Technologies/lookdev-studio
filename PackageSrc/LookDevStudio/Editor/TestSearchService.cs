using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor;
using UnityEditor.Search;
using UnityEditor.Searcher;

using System.IO;

public class TestSearchService
{

    static ISearchView searchViewForMaterial;
    static ISearchView searchViewForModel;
    static ISearchView searchViewForTexture;

    //[MenuItem("DEBUG/SearchService/Open the searchers with filters")]
    static void OpenQuickSearcherWithFilters()
    {
        SetAllSearchProvidersDisabled();

        // Only check the files in the project
        SearchService.SetActive("asset", active: true);
        SearchService.Refresh();
        

        searchViewForMaterial = SearchService.ShowWindow();
        searchViewForMaterial.SetSearchText("*.mat");
        searchViewForMaterial.itemIconSize = (float)DisplayMode.Limit;

        searchViewForModel = SearchService.ShowWindow();
        searchViewForModel.SetSearchText("*.fbx or *.obj");
        searchViewForModel.itemIconSize = (float)DisplayMode.List;

        searchViewForTexture = SearchService.ShowWindow();
        searchViewForTexture.SetSearchText("t:texture -*.exr");
        searchViewForTexture.itemIconSize = (float)DisplayMode.Grid;

        searchViewForModel.Focus();

    }

    //[MenuItem("DEBUG/SearchService/Display Selected Items on the MaterialSearchView")]
    static void DisplaySelectedItems()
    {
        if (searchViewForMaterial == null)
        {
            searchViewForMaterial = SearchService.ShowWindow();
            searchViewForMaterial.SetSearchText("*.mat");
            searchViewForMaterial.itemIconSize = (float)DisplayMode.Limit;
        }

        if (searchViewForMaterial.selection.Count == 0)
        {
            Debug.LogError("No Selections");
            return;
        }

        foreach(SearchItem sItem in searchViewForMaterial.selection)
        {
            string assetPath = AssetDatabase.GetAssetPath(sItem.ToObject());

            Debug.Log("ASSET TYPE : " + sItem.ToObject().GetType());
            Debug.Log("ASSET PATH : " + assetPath);
        }

    }

    //[MenuItem("DEBUG/SearchService/Get registered SearchProvider list")]
    static void GetSearchProviderList()
    {
        int index = 0;

        foreach (SearchProvider searchProvider in SearchService.Providers)
        {
            Debug.LogWarning("Index : " + index.ToString());
            Debug.Log("\tName : " + searchProvider.name);
            Debug.Log("\tIs Active : " + searchProvider.active);
            Debug.Log("\tID : " + searchProvider.id);
            Debug.Log("\tFilter ID : " + searchProvider.filterId);
            Debug.Log("\tShow Details : " + searchProvider.showDetails);
            Debug.Log(string.Empty);
            index++;
        }
    }


    public static void SetAllSearchProvidersDisabled()
    {
        foreach (SearchProvider searchProvider in SearchService.Providers)
        {
            SearchService.SetActive(searchProvider.id, active: false);
        }

        SearchService.Refresh();
    }

    

}
