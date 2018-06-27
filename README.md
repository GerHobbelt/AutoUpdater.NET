# AutoUpdater.NET (Self-Driven)  [![Build status](https://ci.appveyor.com/api/projects/status/02fv57hxutu4mnq2?svg=true)](https://ci.appveyor.com/project/asarmiento13315/autoupdater-net) [![Donate](https://img.shields.io/badge/Donate-PayPal-green.svg)](http://paypal.me/K1k0Soft)

AutoUpdater.NET is a class library that allows .NET developers to easily add auto update functionality to their classic desktop application projects, with capability to run in unattended mode. (Originally forked from ravibpatel/AutoUpdater.NET)

## The NuGet package  [![NuGet](https://img.shields.io/nuget/v/Autoupdater.NET.SelfDriven.svg)](https://www.nuget.org/packages/Autoupdater.NET.SelfDriven/) [![NuGet](https://img.shields.io/nuget/dt/Autoupdater.NET.SelfDriven.svg)](https://www.nuget.org/packages/Autoupdater.NET.SelfDriven/)

`https://www.nuget.org/packages/Autoupdater.NET.SelfDriven/`

    PM> Install-Package Autoupdater.NET.SelfDriven

## How it works

AutoUpdater.NET downloads the XML file containing update information from your server. It uses this XML file to get the information about the latest version of the software. If latest version of the software is greater than current version of the software installed on User's PC then AutoUpdater.NET can let the user decide running the avaliable update immediately or maybe later, and it can even run in unattended mode if your application require it. 
In any case, AutoUpdater.NET will download the update file (Installer) from URL provided in XML file and will launch it. It is a job of installer after this point to carry out the update. But, if you provide zip file URL instead of installer then AutoUpdater.NET will extract the contents of zip file to application directory.

## Using the code

### XML file

AutoUpdater.NET uses XML file located on a server to get the release information about the latest version of the software. You need to create an XML file like below and then you need to upload it to your server.

````xml
<?xml version="1.0" encoding="UTF-8"?>
<item>
    <version>2.0.0.0</version>
    <url>[url-of-the-package-to-be-downloaded]</url>
</item>
````

There are two things you need to provide in XML file as you can see above.

* version (Required) : You need to provide latest version of the application between version tags. Version should be in X.X.X.X format.
* url (Required): You need to provide URL of the latest version installer file or zip file between url tags. AutoUpdater.NET downloads this file provided here and installs it.
* changelog (Optional): You can provide URL of the change log of your application between changelog tags.
* mandatory (Optional): You can set this to true if you don't want user to postpone or skip this version. Therefore, this will ignore Remind Later and Skip options and hide both correspondent buttons on update dialog.
* args (Optional): You can provide command line arguments for the executable installer between this tag. You can include %path% with your command line arguments, it will be replaced by path of the directory where currently executing application resides.
* checksum (Optional): You can provide the checksum for the update file between this tag. If you do this AutoUpdater.NET will compare the checksum of the downloaded file before executing the update process to check the integrity of the file. You can provide algorithm attribute in the checksum tag to specify which algorithm should be used to generate the checksum of the downloaded file. Currently, MD5, SHA1, SHA256, SHA384, and SHA512 are supported.

````xml
<checksum algorithm="MD5">Update file Checksum</checksum>
````

### Adding one line to make it work

After you done creating and uploading XML file, It is very easy to add a auto update functionality to your application. First you need to add following line at the top of your form.

````csharp
using AutoUpdaterDotNET;
````

Now you just need to add following line to your main form constructor or in Form_Load event. You can add this line anywhere you like. If you don't like to check for update when application starts then you can create a Check for update button and add this line to Button_Click event.

````csharp
AutoUpdater.Start("[url-of-your-latest-version-information-xml-file]");
````

Start method of AutoUpdater class takes URL of the XML file you uploaded to server as a parameter.

    AutoUpdater.Start should be called from UI thread.

## Configuration Options

You can setup AutoUpdater.NET to better fit your requirements by first using its initializer..

````csharp
AutoUpdater.InitSettings
            .SetAppCastURL("[url-of-your-latest-version-information-xml-file]")
            .EnableUnattendedMode()
            .EnableMandatory()
            .EnableReportErrors()
            .Initialize();
````

### Pre-set the URL of the XML file containing update information

Using this lets you call the start method without having to specify the URL.

````csharp
    .SetAppCastURL("[url-of-your-latest-version-information-xml-file]")
````

### Enable the Unattended Mode

AutoUpdater.NET will run self-driven using your settings and without expecting any user interaction. No modal user dialogs will be shown, it will only log the process state if some reporting level is enabled.

````csharp
    .EnableUnattendedMode()
````

### Disable Skip Option

If you don't want to show the Skip button on Update form then just add following line.

````csharp
    .DisableShowSkipOption()
````

### Disable Remind Later Option

If you don't want to show the Remind Later button on Update form then just add following line.

````csharp
    .DisableShowRemindLaterOption()
````

### Ignore previous Remind Later or Skip settings

If you want to ignore previously set Remind Later and Skip settings then you can use Enable Mandatory setting. It will also hide Skip and Remind Later button. When set this in code then value of Mandatory in your XML file will be ignored.

````csharp
    .EnableMandatory()
````

### Run update process without Administrator privileges

If your application doesn't need administrator privileges to replace old version then you can disable it.

````csharp
    .DisableRunUpdateAsAdmin()
````

### Open Download Page

If you don't want to download the latest version of the application and just want to open the URL between url tags of your XML file then add following line.

````csharp
    .EnableOpenDownloadPage()
````

This kind of scenario is useful if you want to show some information to users before they download the latest version of an application.

### Remind Later

If you don't want users to select Remind Later time when they press the Remind Later button of update dialog then you need to add following lines.

````csharp
    .DisableLetUserSelectRemindLater()
    .SetRemindLaterAt([interval])
    .SetRemindLaterTimeSpan([interval-span])
````

### Enable Reporting

You can turn on error reporting by adding below code. If you do this AutoUpdater.NET will show error message, like if it can't get to the XML file from web server.

````csharp
    .EnableReportErrors()
````

You can turn on info reporting by adding below code. AutoUpdater.NET will also show error message, if there is no update available.

````csharp
    .EnableReportInfos()
````

Or you just can enable both reporting levels using

````csharp
    .EnableReportAll()
````

This settings are more useful when running in unattended mode to let you trace the process state. 
You can even set the path where the default logger will use as storage and if you want to extend the logging capabilities you can provide your custom logger implementation.

````csharp
    .SetTheDefaultLogFolder("[path-to-log-folder]")
````
````csharp
    .SetALogger("[your-impl-of-ILogger]")
````

In above example when user press Remind Later button of update dialog, It will remind user for update after 2 days.

### Proxy Server

If your XML and Update file can only be used from certain Proxy Server then you can use following settings to tell AutoUpdater.NET to use that proxy. Currently, if your Changelog URL is also restricted to Proxy server then you should omit changelog tag from XML file cause it is not supported using Proxy Server.

````csharp
var proxy = new WebProxy("ProxyIP:ProxyPort", true) 
{
    Credentials = new NetworkCredential("ProxyUserName", "ProxyPassword")
};

//...
    .SetProxy(proxy)
````

### Specify where to download the update file

You can specify where you want to download the update file by assigning DownloadPath field as shown below. It will be used for ZipExtractor too.

````csharp
    .SetDownloadPath("[path-download-folder]");
````


## Check updates frequently

You can call Start method inside Timer to check for updates frequently.

### WinForms

````csharp
System.Timers.Timer timer = new System.Timers.Timer
{
    Interval = 2 * 60 * 1000,
    SynchronizingObject = this
};
timer.Elapsed += delegate
{
    AutoUpdater.Start("[url-of-your-latest-version-information-xml-file]");
};
timer.Start();
````

### WPF

````csharp
DispatcherTimer timer = new DispatcherTimer {Interval = TimeSpan.FromMinutes(2)};
timer.Tick += delegate
{
    AutoUpdater.Start("[url-of-your-latest-version-information-xml-file]");
};
timer.Start();
````

## Handling Application exit logic manually

If you like to handle Application exit logic yourself then you can use SetAnApplicationExitEventHandler like below. This is very useful if you like to do something before closing the application.

````csharp
    .SetAnApplicationExitEventHandler(AutoUpdater_ApplicationExitEvent);

//...
private void AutoUpdater_ApplicationExitEvent()
{
    Text = @"Closing application...";
    Thread.Sleep(5000);
    Application.Exit();
}
````

## Handling updates manually

Sometimes as a developer you need to maintain look and feel for the entire application similarly or you just need to do something before update. In this type of scenarios you can handle the updates manually by providing your own handler. You can do it using SetACustomUpdateCheckEventHandler and DownloadAndRunTheUpdate.

````csharp
    .SetACustomUpdateCheckEventHandler(AutoUpdaterOnUpdateCheckEvent);

private void AutoUpdaterOnUpdateCheckEvent(UpdateInfoEventArgs updateInfo)
{
    if (updateInfo != null)
    {
        if (updateInfo.IsUpdateAvailable)
        {
            DialogResult dialogResult;
            if (updateInfo.Mandatory)
            {
                dialogResult =
                    MessageBox.Show(
                        $@"There is new version {updateInfo.CurrentVersion} available. You are using version {updateInfo.InstalledVersion}. This is required update. Press Ok to begin updating the application.", @"Update Available",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
            }
            else
            {
                dialogResult =
                    MessageBox.Show(
                        $@"There is new version {updateInfo.CurrentVersion} available. You are using version {
                                updateInfo.InstalledVersion
                            }. Do you want to update the application now?", @"Update Available",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information);
            }

            if (dialogResult.Equals(DialogResult.Yes))
            {
                try
                {
                    // this also will ask the app to exit after launching the update
                    AutoUpdater.DownloadAndRunTheUpdate() 
                }
                catch (Exception exception)
                {
                    MessageBox.Show(exception.Message, exception.GetType().ToString(), MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }
        else
        {
            MessageBox.Show(@"There is no update available please try again later.", @"No update available",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
    else
    {
        MessageBox.Show(
                @"There is a problem reaching update server please check your internet connection and try again later.",
                @"Update check failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
````

When you do this it will execute the code in above event when AutoUpdater.Start method is called instead of showing the update dialog. UpdateInfoEventArgs object carries all the information you need about the update. If its null then it means AutoUpdater.NET can't reach the XML file on your server. UpdateInfoEventArgs has following information about the update.

* IsUpdateAvailable (bool) :  If update is available then returns true otherwise false.
* DownloadURL (string) : Download URL of the update file..
* ChangelogURL (string) : URL of the webpage specifying changes in the new update.
* CurrentVersion (Version) : Newest version of the application available to download.
* InstalledVersion (Version) : Version of the application currently installed on the user's PC.
* Mandatory (bool) : Shows if the update is required or optional.

## Handling parsing logic manually

If you want to use other format instead of XML as a AppCast file then you need to handle the parsing logic by using SetAParseUpdateInfoEventHandler. You can do it as follows.

````csharp
    .SetAParseUpdateInfoEventHandler(AutoUpdaterOnParseUpdateInfoEvent);

//...
private void AutoUpdaterOnParseUpdateInfoEvent(ParseUpdateInfoEventArgs args)
{
    dynamic json = JsonConvert.DeserializeObject(args.RemoteData);
    args.UpdateInfo = new UpdateInfoEventArgs
    {
        CurrentVersion = json.version,
        ChangelogURL = json.changelog,
        Mandatory = json.mandatory,
        DownloadURL = json.url
    };
}
````

### JSON file used in the Example above

````json
{
    "version":"2.0.0.0", 
    "url":"...",
    "changelog":"...",
    "mandatory":true
}
````


## Some Extensibility


To let you replace the built-in implementations of user dialogs shown through out the process provide any of the following factories to make AutoUpdater.NET to user your custom ones.

### User Update Form

Inspect the definition of UpdateFormPresenterFactory and UpdateFormPresenter interfaces for more details.

````json
    .SetAnUpdateFormPresenterFactory(..)
````

### Update Download Form

Inspect the definition of DownloadPresenterFactory and DownloadPresenter interfaces for more details.

````json
    .SetADownloadPresenterFactory(..)
````


To let you replace the built-in implementations responsable of downloading and processing the update.

### Update Downloader

Inspect the definition of FileDownloaderFactory and FileDownloader interfaces for more details.

````json
    .SetAnUpdateDownloaderFactory(..)
````

### Update Launcher

Inspect the definition of UpdateLauncherFactory and UpdateLauncher interfaces for more details.

````json
    .SetAnUpdateLauncherFactory(..)
````