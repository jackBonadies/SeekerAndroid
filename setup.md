### Set up for you own apps
This guide will show you how to set up an F-Droid repo with this tool. It makes some assumptions you need to know about:
* You use GitHub to host your app repository
* You create GitHub releases for your app that contain exactly one artifact with an `.apk` suffix
  * My recommendation is to create a GitHub Actions workflow in your app repo that builds & signs your APK, then publishes it as a release (maybe as a draft release so you have more control). If you want to see how I did it with a Flutter app, go [here](https://github.com/xarantolus/notality/blob/main/.github/workflows/android_build.yml) 
* Your release tag names are something like `v1.2.3` (not a hard requirement, but recommended)

### Install & initialize your F-Droid repository
1. First of all, clone this repository and delete everything from the `fdroid` directory. This deletes my repo files; you can now use it for your own apps.

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
    <ul>
      <li>Create, edit, delete and reorder notes</li>
      <li>Automatic dark/light mode depending on the system-wide setting</li>
      <li>Localization for English and German</li>
    </ul>

another_app:
  git: https://github.com/xarantolus/myotherapp
```

If the repository has APK releases, they should be imported into this repo the next time GitHub Actions run.

