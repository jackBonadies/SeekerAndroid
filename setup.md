### Set up for you own apps
First of all, clone this repository and delete everything from the `fdroid/repo` directory.

Then run `fdroid init` in the `fdroid` subdirectory.

Edit the generated `fdroid/config.py`. The comments will tell you a lot, but make sure the `repo_url` looks something like this (it should include your username instead of `xarantolus`):

    repo_url = "https://raw.githubusercontent.com/xarantolus/fdroid/main/fdroid/repo"

You should also set `archive_older` to `0`.


After finishing your edits, you can run the following command:

    base64 config.yml > out.txt

Copy the contents of `out.txt` and set up a new repository secred called `CONFIG_YML`. Just paste the content of `out.txt` into the value field.

Then do the same with the keystore file:

    base64 keystore.p12 > out.txt

And now create another secret with the name `KEYSTORE_P12`, and again paste the content of `out.txt`.


Then go to `https://github.com/settings/tokens/new?description=f-droid repo` and generate a token, no scopes required
