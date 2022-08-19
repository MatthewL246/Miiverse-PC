# Miiverse-PC

This is a Miiverse portal client for PCs, specifically for Pretendo Juxtaposition. It also has some other useful features, such as downloading a Pretendo user's profile data.

## Installation

1. Download the [lastest release](https://github.com/MatthewL246/Miiverse-PC/releases) zip file that matches your PC's architecture, and extract this zip to where you want to run the app from.
2. Download the latest installer by opening the [Windows App SDK Downloads page](https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/downloads) and scrolling down to the latest Windows App SDK 1.1 release. Under runtime downloads, download and run the installer that matches your PC's architecture. Note that this includes a .NET 6.0 runtime, so you don't need to install it seperately.
3. Check your PC's apps list. If you have an app called "Microsoft Edge WebView2 Runtime" installed (**not** just "Microsoft Edge"), you're good to go. If not, download the [Microsoft Edge WebView2 runtime](https://go.microsoft.com/fwlink/p/?LinkId=2124703) and install that.
4. Run the `Miiverse-PC.exe` app inside the extracted zip folder.

*Note: Hopefully, this installation process gets simpler in the future. But for now, this is all necessary to make sure the app runs.*

## Getting the password hash

1. Log in to your Pretendo account at [pretendo.network](https://pretendo.network/account) (or an different Pretendo server that is hosting the website).
2. Click the "Download account files" button and save the zip.
3. Extract the zip and open the `account.dat` file in any text editor. The password hash is the 64-character hash after `AccountPasswordCache=`.
