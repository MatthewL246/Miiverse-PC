# Miiverse-PC

This is a Miiverse portal client for PCs, specifically for Pretendo Juxtaposition. It also has some other useful features, such as downloading a Pretendo user's profile data.

## ⚠️ Notice: Unfortunately, this doesn't work anymore.

In the 2 years since I created this, the Pretendo Network account server [added verification checks](https://github.com/PretendoNetwork/account/blob/dev/src/middleware/console-status-verification.ts) that check if the request is coming from a real console or an emulator with a valid console dump.

I might work on fixing this in the future, but passing these checks would require some major changes and make this app require a console dump.

## Installation

### Releases

Choose whether you want to use the MSIX installer or the portable zip file. The installer is easier to use (after installing the certificate for the first time) and automatically sets up a Start Menu and Settings installed apps entry, while the portable version makes it easier to quickly try out the app without installing.

#### Installer (.MSIX)

##### Certificate installation

First, you need to install and trust my MSIX signing certificate. You only need to do this once **when installing for the first time**, not when updating.

1. Download the release file named `Miiverse-PC_MSIX_Certificate.cer`.
2. Double-click the certificate file, click the details tab, and scroll down to and select "Thumbprint." On the bottom of the dialog, it should display `428f9c7f9ac01e6ad06d38183c3d31065803d977`. **If it does not, DO NOT TRUST THE CERTIFICATE and create an issue in this repo.**
3. If the thumbprint matches, go back to the general tab and click "Install Certificate..."
    1. Select "Local Machine" and then Next. Click Yes on the User Account Control dialog.
    2. Select "Place all certificates in the following store," click Browse, and select the **"Trusted People"** certificate store. Then click OK and Next.
    3. Finally, click Finish to install and trust the certificate.

If you ever want to remove my certificate, open "Manage computer certificates" (`certlm.msc`) in the Start menu. Then open the `Trusted People\Certificates` store and delete any certificates named "MatthewL246."

##### App installation and updates

1. Download the [lastest release](https://github.com/MatthewL246/Miiverse-PC/releases) MSIX file that matches your PC's architecture (x86, x64, or arm64).
2. Simply run the installer and click Install, Update, or Reinstall.

If an "Open with..." dialog shows up when you try to run the installer, you need to install the [App Installer](https://apps.microsoft.com/store/detail/app-installer/9NBLGGH4NNS1) first. (If you don't have access to the Microsoft Store, it's also availible from [Microsoft's GitHub](https://github.com/microsoft/winget-cli/releases), but you'll need to use `Add-AppxPackage` in PowerShell to install it).

#### Portable (.ZIP)

1. Download the [lastest release](https://github.com/MatthewL246/Miiverse-PC/releases) zip file that matches your PC's architecture (x86, x64, or arm64).
2. Extract this zip to wherever you want to run the app from.
3. Run the `Miiverse-PC.exe` app inside the extracted zip folder.
4. Optionally, create shortcuts to the app on your desktop or in the Start menu (`%APPDATA%\Microsoft\Windows\Start Menu\Programs`).

### Latest CI builds

**⚠️ CI builds may be unstable. Use at your own risk! ⚠️** 

You may download the latest CI build to get the most recent updates.

1. Open the [GitHub Actions build page](https://github.com/MatthewL246/Miiverse-PC/actions/workflows/dotnet-build.yml).
2. Select the most recent **successful** run.
3. Download either the packaged build (*the installer MSIX*) or the unpackaged build (*the portable ZIP*).

Note that the CI build MSIX installer uses a different signing certificate than the release builds for security reasons, so you will need to trust that certificate too. Follow the same steps as installing the release certificate but using the Actions certificate instead. The Actions certificate thumbprint should be `0BF22E6635F39155A64E82AFED0077A771E4D164`.

## Getting the password hash

1. Log in to your Pretendo account at [pretendo.network](https://pretendo.network/account) (or a different Pretendo server that is hosting the website).
2. Click the "Download account files" button and save the zip.
3. Extract the zip and open the `account.dat` file in any text editor. The password hash is the 64-character hash after `AccountPasswordCache=`.
