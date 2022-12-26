# fdroid
This repository hosts an [F-Droid](https://f-droid.org/) repo for my apps. This allows you to install and update apps very easily.

### Apps

<!-- This table is auto-generated. Do not edit -->
| Icon | Name | Description | Version |
| --- | --- | --- | --- |
| <a href="https://github.com/jackBonadies/SeekerAndroid"><img src="fdroid/repo/icons/com.companyname.andriodapp1.91.png" alt="Seeker icon" width="36px" height="36px"></a> | [**Seeker**](https://github.com/jackBonadies/SeekerAndroid) | Android client for the Soulseek peer-to-peer network | 2.9.12 (91) |
<!-- end apps table -->

Please note that some apps published here might contain [Anti-Features](https://f-droid.org/en/docs/Anti-Features/). If you can't find an app by searching for it, you can go to settings and enable "Include anti-feature apps".

### How to use
1. At first, you should [install the F-Droid app](https://f-droid.org/), it's an alternative app store for Android.
2. Now you can copy the following [link](https://raw.githubusercontent.com/jackbonadies/seekerandroid/fdroid/fdroid/repo?fingerprint=D9613C106A63D632F0F15597F4A91C276D3C6ED152F19518C3A5573BF8DA2375), then add this repository to your F-Droid client:

    ```
    https://raw.githubusercontent.com/jackbonadies/seekerandroid/fdroid/fdroid/repo?fingerprint=D9613C106A63D632F0F15597F4A91C276D3C6ED152F19518C3A5573BF8DA2375
    ```

    Alternatively, you can also scan this QR code:

    <p align="center">
      <img src=".github/qrcode.png?raw=true" alt="F-Droid repo QR code"/>
    </p>

3. Open the link in F-Droid. It will ask you to add the repository. Everything should already be filled in correctly, so just press "OK".
4. You can now install the app, e.g. start by searching for "Seeker" in the F-Droid client.

### For developers
If you are a developer and want to publish your own apps right from GitHub Actions as an F-Droid repo, you can use the template [efreak/fdroid-action](https://github.com/efreak/fdroid-action) or fork [xarantolus/fdroid](https://github.com/xarantolus/fdroid) and see  [the documentation](setup.md) for more information on how to set it up.

### [License](LICENSE)
The license is for the files in this repository, *except* those in the `fdroid` directory. These files *might* be licensed differently; you can use an F-Droid client to get the details for each app.
