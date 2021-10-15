### Set up for you own apps
This guide will show you how to set up an F-Droid repo with this tool. It makes some assumptions you need to know about:
* You use GitHub to host the repositories of your app(s)
* You create GitHub releases for your app(s) that contain exactly one artifact with an `.apk` suffix
  * My recommendation is to create a GitHub Actions workflow in your app repo that builds & signs your APK, then publishes it as a release (maybe as a draft release so you have more control). If you want to see how I did it with a Flutter app, go [here](https://github.com/xarantolus/notality/blob/main/.github/workflows/android_build.yml).
* Your release tag names are something like `v1.2.3` (recommended, but should work anyways regardless)

This tool does not build your apps from source. It assumes that the build process runs in the application's repository.

When building/releasing a new version of your app, you need to make sure that you update not only the `versionName`, but also the `versionCode`. It seems like the latter is preferred by F-Droid for comparing versions. 
* In Flutter, you have something like `version: 1.2.3+4` in your `pubspec.yaml` file. The `1.2.3` is the `versionName`, the `versionCode` is after the `+`, so `4` in this case. You should update both for F-Droid to recognize an update.

### Install & initialize your F-Droid repository
1. First of all, clone this repository and delete everything from the `fdroid` directory. This deletes my repo files; you can now use it for your own apps. If you want to reduce the size of the repository, you can also delete the `.git` directory, then `git init` again (you need to force-push/create a new repo after that). 

2. Now you can [install the F-Droid server tools](https://f-droid.org/en/docs/Installing_the_Server_and_Repo_Tools/) by running the following:

        sudo add-apt-repository ppa:fdroid/fdroidserver
        sudo apt-get update
        sudo apt-get install fdroidserver

    Make sure that you install version `2.x` of the `fdroidserver` package. In the previous step we added the repository because the `fdroidserver` package I found at first (in the default repo) was outdated; so just make sure it's version 2 (you can also check with `apt-get -s install fdroidserver`).

    We only need these tools once to set up the repository. After these steps you can delete them, as now GitHub Actions will manage everything.

3. Then run `fdroid init` in the `fdroid` subdirectory:

        cd fdroid && fdroid init

    This creates two files: `fdroid/config.yml` and `fdroid/keystore.p12`. The first one is the configuration file for your repository, the second one is a keystore file (these are used for signing apps when building, but this tool doesn't build apps).

    Edit the generated `fdroid/config.yml`. The comments will tell you a lot, but make sure the `repo_url` looks something like this (it should include your username instead of `xarantolus`):

    ```yml
    repo_url: https://raw.githubusercontent.com/xarantolus/fdroid/main/fdroid/repo
    ```

    You should also set `archive_older` to `0` to disable the archive:

    ```yml
    archive_older: 0
    ```

4. Open your GitHub repository, go to Settings and then to Secrets. We will create a few "Repository secrets" now (do not mix them up with the "Environment secrets", we don't want those!)

5. After finishing your edits to the two files, you can run the following command:

        base64 config.yml > out.txt

    Now copy the contents of `out.txt` and set up a new repository secred called `CONFIG_YML`. Just paste the content of `out.txt` into the value field.

    Then do the same with the keystore file:

        base64 keystore.p12 > out.txt

    And now create another secret with the name `KEYSTORE_P12`, and again paste the content of `out.txt`.

6. Then open [this page](https://github.com/settings/tokens/new?description=f-droid%20repo) and generate a new GitHub personal access token without any scopes. Set the expiration date to "No expiration" (or really any timeframe on how often you want to manually update this secret). Copy the token and set it as the `GH_ACCESS_TOKEN` repository secret.
   
That should be it. You can now also generate a new QR code for your repo using online tools, then replace the file in `.github/qrcode.png`. And of course, you should now add your apps!

### Add a new app
Now you can edit the `apps.yaml` file to include a new app. Usually you just need to input the GitHub link and everything should work:

```yml
notality:
  git: https://github.com/xarantolus/notality
  description: |
    Notality is a very simple note taking app. I mostly built it to learn how Flutter works, but it's functional and you can use it.

    <b>Features</b>

    - Create, edit, delete and reorder notes
    - Automatic dark/light mode depending on the system-wide setting
    - Localization for English and German

  # As described on https://f-droid.org/en/docs/Build_Metadata_Reference/#Categories,
  # you can use any name here, but you should look at the existing categories first
  categories: 
    - Writing

another_app:
  git: https://github.com/xarantolus/myotherapp
```

If the repository has APK releases, they should be imported into this repo the next time GitHub Actions run.

### Metadata and screenshots
Metadata can be added in two places: the `apps.yaml` file and the app repositories.

#### Metadata file
**Description**: As described in [Add a new app](#add-a-new-app), you can set a git URL and a description in the `apps.yaml` file

**Categories**: A list of categories, preferably one of the [categories already listed in the official repo](https://f-droid.org/en/docs/Build_Metadata_Reference/#Categories)

#### Metadata from the repository
**Screenshots**: This tool will make any file from the git repository for which the path contains `screenshot` available as screenshot. Basically, if you run `find .  -type f | grep -i screenshot` in your app repo you should find all files that will be used.

**Changelog**: To display a "what's new" changelog in F-Droid, you just need to fill out the body/text of the GitHub release.

**License**: The License `spdx_id` given by GitHub. Make sure GitHub recognizes the license type of your app. 

**Tag line**: The tag line of the app shown in F-Droid is the same text as the repository description on GitHub.


### Repository URL
When you link to your repository, you can also add the fingerprint to the URL.
To get the fingerprint, you need to look at the `fdroid` command output (or search for the following lines in GitHub Actions):

    2021-10-11 06:01:21,726 INFO: Creating signed index with this key (SHA256):
    2021-10-11 06:01:21,726 INFO: 08 08 98 AE 43 09 AE CE B5 89 15 E4 3A 4B 7C 4A 3E 2C DA 40 C9 17 38 E2 C0 2F 58 33 9A B2 FB D7

Just remove all spaces from after "INFO" in the second line and you'll end up with your fingerprint:

    080898AE4309AECEB58915E43A4B7C4A3E2CDA40C91738E2C02F58339AB2FBD7

Now add it to your repo URL (add a `?fingerprint=`, then your key): 

    https://raw.githubusercontent.com/xarantolus/fdroid/main/fdroid/repo?fingerprint=080898AE4309AECEB58915E43A4B7C4A3E2CDA40C91738E2C02F58339AB2FBD7

You should of course replace the username in the URL. This is the URL your users should add to the F-Droid client. You can also generate a QR code for this URL.
