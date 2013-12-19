/*
* LICENSE: https://raw.github.com/apimash/StarterKits/master/LicenseTerms-SampleApps%20.txt
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using XPlatformCloudKit.Models;
using XPlatformCloudKit.ViewModels;
using System.Windows.Media;
using System.ComponentModel;
using Microsoft.Phone.Tasks;
using XPlatformCloudKit.Helpers;
using XPlatformCloudKit.Resources;
using Cirrious.MvvmCross.WindowsPhone.Views;

namespace XPlatformCloudKit.Views
{
    public partial class ItemDescriptionView : MvxPhonePage
    {
        private int selectedIndex;

        public ItemDescriptionView()
        {
            InitializeComponent();
            //DataContext = new ItemDescriptionViewModel();
            selectedIndex = AppState.SelectedGroup.IndexOf(AppState.SelectedItem);
            
            if(selectedIndex == 0)
                ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IsEnabled = false;
            if(selectedIndex == AppState.SelectedGroup.Count - 1)
                ((ApplicationBarIconButton)ApplicationBar.Buttons[2]).IsEnabled = false;
        }

        private void NextButton_Click(object sender, EventArgs e)
        {
            selectedIndex++;
            ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IsEnabled = true;

            if (selectedIndex == AppState.SelectedGroup.Count - 1)
                ((ApplicationBarIconButton)ApplicationBar.Buttons[2]).IsEnabled = false;
            
            ((ItemDescriptionViewModel)DataContext).SelectedItem = AppState.SelectedGroup[selectedIndex];
        }

        private void PreviousButton_Click(object sender, EventArgs e)
        {
            selectedIndex--;
            ((ApplicationBarIconButton)ApplicationBar.Buttons[2]).IsEnabled = true;

            if (selectedIndex == 0)
                ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IsEnabled = false;

            ((ItemDescriptionViewModel)DataContext).SelectedItem = AppState.SelectedGroup[selectedIndex];
        }

        private void ShareButton_Click(object sender, EventArgs e)
        {
            ShareStatusTask shareStatusTask = new ShareStatusTask();
            shareStatusTask.Status = "Checking out " + 
                                     ((ItemDescriptionViewModel)DataContext).SelectedItem.Title +
                                     " on " + AppSettings.ApplicationName;
            shareStatusTask.Show();
        }

        private WebBrowser browser;

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.Back)
            {
                browser.NavigateToString("<HTML></HTML>");//navigate to blank page
            }
            base.OnNavigatingFrom(e);
        }

        private void WireUpWebBrowser(object sender, RoutedEventArgs e)
        {
            browser = sender as WebBrowser;
            browser.IsScriptEnabled = true;
            browser.ScriptNotify += browser_ScriptNotify;
            if (browser != null) browser.Navigating += browser_Navigating;
            ((ItemDescriptionViewModel)DataContext).PropertyChanged += ItemDescriptionView_PropertyChanged;
            LoadWebContent();
        }

        void browser_ScriptNotify(object sender, NotifyEventArgs e)
        {
            if (e.Value.StartsWith("launchPhoneCall:"))
            {
                string phoneNumber = e.Value.Remove(0, 16);
                PhoneCallTask phoneCallTask = new PhoneCallTask();
                phoneCallTask.PhoneNumber = phoneNumber;
                phoneCallTask.Show();
            }
        }
        void LaunchPhoneCall(string number)
        {
            PhoneCallTask phoneCallTask = new PhoneCallTask();
            phoneCallTask.PhoneNumber = number;
            phoneCallTask.Show();
        }

        void ItemDescriptionView_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SelectedItem")
                LoadWebContent();
        }

        private void LoadWebContent()
        {
            var selectedItem = ((ItemDescriptionViewModel)DataContext).SelectedItem;


            var bc = AppSettings.BackgroundColorOfDescription[0] == '#' ? AppSettings.BackgroundColorOfDescription : FetchBackgroundColor();

            var fc = AppSettings.FontColorOfDescription[0] == '#' ? AppSettings.FontColorOfDescription : FetchFontColor();

            string scriptOptions = string.Empty;
            string disableHyperLinksJS = "<script type='text/javascript'>window.onload = function() {   var anchors = document.getElementsByTagName(\"a\"); for (var i = 0; i < anchors.length; i++) { anchors[i].onclick = function() {return(false);}; }};</script>";
            string disableOpeningHyperLinksInNewTabJS = "<script type='text/javascript'>window.onload = function() {   var anchors = document.getElementsByTagName(\"a\"); for (var i = 0; i < anchors.length; i++) { anchors[i].target = \"_self\"; }};</script>";

            string launchPhoneCallJS = @"<script type='text/javascript'>  function callOutToCSharp(stringParameter){window.external.notify(stringParameter.toLocaleString());} window.onload = function() {   var regex = /((\([0-9]{3}\) |[0-9]{3}-)[0-9]{3}-[0-9]{4})/, replacement = '<input type=""button"" value=""$1"" onclick=""callOutToCSharp(\'launchPhoneCall:$1\');"" />'; function replaceText(el) { if (el.nodeType === 3) { if (regex.test(el.data)) { var temp_div = document.createElement('div'); temp_div.innerHTML = el.data.replace(regex, replacement); var nodes = temp_div.childNodes; while (nodes[0]) { el.parentNode.insertBefore(nodes[0],el); } el.parentNode.removeChild(el); } } else if (el.nodeType === 1) { for (var i = 0; i < el.childNodes.length; i++) { replaceText(el.childNodes[i]);  }  }} replaceText(document.body); } </script>";

            if (AppSettings.DisableHyperLinksInItemDescriptionView)
                scriptOptions = scriptOptions + disableHyperLinksJS;
            if (AppSettings.DisableOpeningHyperLinksInNewTab)
                scriptOptions = scriptOptions + disableOpeningHyperLinksInNewTabJS;

            if (AppSettings.EnableParsingPhoneNumbersPhone8)
                scriptOptions = scriptOptions + launchPhoneCallJS;

            var webcontent = "<HTML>" +
            "<HEAD>" +
            "<meta name=\"viewport\" content=\"width=320, user-scrollable=no\" />"
            +
                scriptOptions
            +
            "<style type='text/css'>a img {border: 0;}</style>" +
            "</HEAD>" +
            "<BODY style=\"background-color:" + bc + ";color:" + fc + "\">" +
            selectedItem.Description +
            "</BODY>" +
            "</HTML>";

            browser.NavigateToString(webcontent);

        }

        void browser_Navigating(object sender, NavigatingEventArgs e)
        {
            e.Cancel = true;
            if (e.Uri.IsAbsoluteUri && (e.Uri.Scheme.ToLower() == "http" || e.Uri.Scheme.ToLower() == "https"))
            {
                WebBrowserTask task = new WebBrowserTask();
                task.Uri = e.Uri;
                task.Show(); 
            }
        }

        private string FetchBackgroundColor()
        {
            return IsBackgroundBlack() ? "#000" : "#fff";
        }

        private string FetchFontColor()
        {
            return IsBackgroundBlack() ? "#fff" : "#000";
        }

        private static bool IsBackgroundBlack()
        {
            return FetchBackGroundColor() == "#FF000000";
        }

        private static string FetchBackGroundColor()
        {
            var mc = (Color)Application.Current.Resources["PhoneBackgroundColor"];
            string color = mc.ToString();
            return color;
        }

        private void ApplicationBarIconButton_Click(object sender, EventArgs e)
        {

        }
    }
}