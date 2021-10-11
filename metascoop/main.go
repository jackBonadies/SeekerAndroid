package main

import (
	"context"
	"errors"
	"flag"
	"fmt"
	"io"
	"io/fs"
	"log"
	"metascoop/apps"
	"metascoop/file"
	"metascoop/git"
	"net/http"
	"os"
	"os/exec"
	"path/filepath"
	"strings"
	"time"

	"github.com/google/go-github/v39/github"
	"golang.org/x/oauth2"
)

func main() {
	var (
		appsFilePath = flag.String("ap", "apps.yaml", "Path to apps.yaml file")
		repoDir      = flag.String("rd", "fdroid/repo", "Path to fdroid \"repo\" directory")
		accessToken  = flag.String("pat", "", "GitHub personal access token")

		debugMode = flag.Bool("debug", false, "Debug mode won't run the fdroid command")
	)
	flag.Parse()

	fmt.Println("::group::Initializing")

	appsList, err := apps.ParseAppFile(*appsFilePath)
	if err != nil {
		log.Fatalf("parsing given app file: %s\n", err.Error())
	}

	var authenticatedClient *http.Client = nil
	if *accessToken != "" {
		ctx := context.Background()
		ts := oauth2.StaticTokenSource(
			&oauth2.Token{AccessToken: *accessToken},
		)
		authenticatedClient = oauth2.NewClient(ctx, ts)
	}
	githubClient := github.NewClient(authenticatedClient)

	var haveError bool

	fdroidIndexFilePath := filepath.Join(*repoDir, "index-v1.json")

	initialFdroidIndex, err := apps.ReadIndex(fdroidIndexFilePath)
	if err != nil {
		log.Fatalf("reading f-droid repo index: %s\n", err.Error())
	}

	err = os.MkdirAll(*repoDir, 0o644)
	if err != nil {
		log.Fatalf("creating repo directory: %s\n", err.Error())
	}

	fmt.Println("::endgroup::")

	// map[apkName]info
	var apkInfoMap = make(map[string]apps.AppInfo)

	for _, app := range appsList {
		fmt.Printf("::group::App: %s/%s\n", app.Author(), app.Name())

		func() {
			defer fmt.Println("::endgroup::")

			repo, err := apps.RepoInfo(app.GitURL)
			if err != nil {
				log.Printf("Error while getting repo info from URL %q: %s", app.GitURL, err.Error())
				haveError = true
				return
			}

			gitHubRepo, _, err := githubClient.Repositories.Get(context.Background(), repo.Author, repo.Name)
			if err == nil {
				app.Summary = gitHubRepo.GetDescription()

				if gitHubRepo.License != nil && gitHubRepo.License.SPDXID != nil {
					app.License = *gitHubRepo.License.SPDXID
				}
			}

			releases, err := apps.ListAllReleases(githubClient, repo.Author, repo.Name)
			if err != nil {
				log.Printf("Error while listing repo releases for %q: %s\n", app.GitURL, err.Error())
				haveError = true
				return
			}

			for _, release := range releases {
				if release.Prerelease != nil && *release.Prerelease {
					if release.TagName == nil {
						nt := "nil-tagname"
						release.TagName = &nt
					}
					log.Printf("Skipping prerelease %q", *release.TagName)
					continue
				}

				apk := apps.FindAPKRelease(release)
				if apk == nil {
					continue
				}

				appName := apps.GenerateReleaseFilename(app.Name(), *release.TagName)

				appClone := app

				appClone.ReleaseDescription = release.GetBody()

				apkInfoMap[appName] = appClone

				appTargetPath := filepath.Join(*repoDir, appName)

				// If the app file already exists for this version, we continue
				if _, err := os.Stat(appTargetPath); !errors.Is(err, os.ErrNotExist) {
					log.Printf("Already have version %q at %q", *release.TagName, appTargetPath)
					continue
				}

				dlCtx, cancel := context.WithTimeout(context.Background(), 5*time.Minute)
				defer cancel()

				appStream, _, err := githubClient.Repositories.DownloadReleaseAsset(dlCtx, repo.Author, repo.Name, *apk.ID, http.DefaultClient)
				if err != nil {
					log.Printf("Error while downloading app %q (artifact id %d) from from release %q: %s", app.GitURL, *apk.ID, *release.TagName, err.Error())
					haveError = true
					continue
				}

				err = downloadStream(appTargetPath, appStream)
				if err != nil {
					log.Printf("Error while downloading app %q (artifact id %d) from from release %q to %q: %s", app.GitURL, *apk.ID, *release.TagName, appTargetPath, err.Error())
					haveError = true
					continue
				}

				log.Printf("Successfully downloaded %q", appTargetPath)

			}
		}()
	}

	if !*debugMode {
		fmt.Println("::group::F-Droid: Creating metadata stubs")
		// Now, we run the fdroid update command
		cmd := exec.Command("fdroid", "update", "--create-metadata", "--delete-unknown")
		cmd.Stderr = os.Stderr
		cmd.Stdout = os.Stdout
		cmd.Stdin = os.Stdin
		cmd.Dir = filepath.Dir(*repoDir)
		err = cmd.Run()

		if err != nil {
			log.Println("Error while running \"fdroid update -c\":", err.Error())

			fmt.Println("::endgroup::")
			os.Exit(1)
		}
		fmt.Println("::endgroup::")
	}

	fmt.Println("::group::Filling in metadata")

	fdroidIndex, err := apps.ReadIndex(fdroidIndexFilePath)
	if err != nil {
		log.Fatalf("reading f-droid repo index: %s\n::endgroup::\n", err.Error())
	}

	// directory paths that should be removed after updating metadata
	var toRemovePaths []string

	walkPath := filepath.Join(filepath.Dir(*repoDir), "metadata")
	err = filepath.WalkDir(walkPath, func(path string, d fs.DirEntry, err error) error {
		if err != nil || d.IsDir() || !strings.HasSuffix(path, ".yml") {
			return err
		}

		pkgname := strings.TrimSuffix(filepath.Base(path), ".yml")

		meta, err := apps.ReadMetaFile(path)
		if err != nil {
			log.Printf("Reading meta file %q: %s", path, err.Error())
			return nil
		}

		latestPackage, ok := fdroidIndex.FindLatestPackage(pkgname)
		if !ok {
			return nil
		}

		apkInfo, ok := apkInfoMap[latestPackage.ApkName]
		if !ok {
			return nil
		}

		// Now update with some info

		setIfEmpty(meta, "AuthorName", apkInfo.Author())
		setIfEmpty(meta, "Name", apkInfo.Name())
		setIfEmpty(meta, "SourceCode", apkInfo.GitURL)
		setIfEmpty(meta, "License", apkInfo.License)
		setIfEmpty(meta, "Description", apkInfo.Description)

		var summary = apkInfo.Summary
		// See https://f-droid.org/en/docs/Build_Metadata_Reference/#Summary for max length
		const maxSummaryLength = 80
		if len(summary) > maxSummaryLength {
			summary = summary[:maxSummaryLength-3] + "..."
		}

		setIfEmpty(meta, "Summary", summary)

		meta["CurrentVersion"] = latestPackage.VersionName
		meta["CurrentVersionCode"] = latestPackage.VersionCode

		err = apps.WriteMetaFile(path, meta)
		if err != nil {
			log.Printf("Writing meta file %q: %s", path, err.Error())
			return nil
		}

		if apkInfo.ReleaseDescription != "" {
			destFilePath := filepath.Join(walkPath, latestPackage.PackageName, "en-US", "changelogs", fmt.Sprintf("%d.txt", latestPackage.VersionCode))

			err = os.MkdirAll(filepath.Dir(destFilePath), os.ModePerm)
			if err != nil {
				log.Printf("Creating directory for changelog file %q: %s", destFilePath, err.Error())
				return nil
			}

			err = os.WriteFile(destFilePath, []byte(apkInfo.ReleaseDescription), os.ModePerm)
			if err != nil {
				log.Printf("Writing changelog file %q: %s", destFilePath, err.Error())
				return nil
			}
		}

		gitRepoPath, err := git.CloneRepo(apkInfo.GitURL)
		if err != nil {
			log.Printf("Cloning git repo from %q: %s", apkInfo.GitURL, err.Error())
			return nil
		}

		metadata, err := apps.FindMetadata(gitRepoPath)
		if err != nil {
			log.Printf("finding metadata in git repo %q: %s", gitRepoPath, err.Error())
			return nil
		}

		screenshotsPath := filepath.Join(walkPath, latestPackage.PackageName, "en-US", "phoneScreenshots")

		_ = os.RemoveAll(screenshotsPath)

		var sccounter int = 1
		for _, sc := range metadata.Screenshots {
			var ext = filepath.Ext(sc)
			if ext == "" {
				continue
			}

			var newFilePath = filepath.Join(screenshotsPath, fmt.Sprintf("%d%s", sccounter, ext))

			err = os.MkdirAll(filepath.Dir(newFilePath), os.ModePerm)
			if err != nil {
				log.Printf("Creating directory for screenshot file %q: %s", newFilePath, err.Error())
				return nil
			}

			err = file.Move(sc, newFilePath)
			if err != nil {
				log.Printf("Moving screenshot file %q to %q: %s", sc, newFilePath, err.Error())
				return nil
			}

			sccounter++
		}

		toRemovePaths = append(toRemovePaths, screenshotsPath)

		log.Printf("Updated metadata file %q", path)

		return nil
	})
	if err != nil {
		log.Printf("Error while walking metadata: %s", err.Error())

		fmt.Println("::endgroup::")
		os.Exit(1)
	}

	fmt.Println("::endgroup::")

	if !*debugMode {
		fmt.Println("::group::F-Droid: Reading updated metadata")

		// Now, we run the fdroid update command again to regenerate the index with our new metadata
		cmd := exec.Command("fdroid", "update", "--delete-unknown")
		cmd.Stderr = os.Stderr
		cmd.Stdout = os.Stdout
		cmd.Stdin = os.Stdin
		cmd.Dir = filepath.Dir(*repoDir)

		err = cmd.Run()
		if err != nil {
			log.Println("Error while running \"fdroid update -c\":", err.Error())

			fmt.Println("::endgroup::")
			os.Exit(1)
		}
		fmt.Println("::endgroup::")
	}

	fmt.Println("::group::Assessing changes")

	// Now at the end, we read the index again
	fdroidIndex, err = apps.ReadIndex(fdroidIndexFilePath)
	if err != nil {
		log.Fatalf("reading f-droid repo index: %s\n::endgroup::\n", err.Error())
	}

	// Now we can remove all paths that were marked for doing so

	for _, rmpath := range toRemovePaths {
		err = os.RemoveAll(rmpath)
		if err != nil {
			log.Fatalf("removing path %q: %s\n", rmpath, err.Error())
		}
	}

	if !apps.HasSignificantChanges(initialFdroidIndex, fdroidIndex) {
		changedFiles, err := git.GetChangedFileNames(*repoDir)
		if err != nil {
			log.Fatalf("getting changed files: %s\n::endgroup::\n", err.Error())
		}

		// If only the index files changed, we ignore the commit
		var insignificant = true
		for _, fname := range changedFiles {
			if !strings.Contains(fname, "index") {
				insignificant = false
				break
			}
		}

		if insignificant {
			log.Println("There were no significant changes, exiting")
			fmt.Println("::endgroup::")
			os.Exit(2)
		}
	}

	fmt.Println("::endgroup::")

	if haveError {
		os.Exit(1)
	}
}

func setIfEmpty(m map[string]interface{}, key string, value string) {
	if value != "" && m[key] == nil || m[key] == "" || m[key] == "Unknown" {
		m[key] = value
	}
}

func downloadStream(targetFile string, rc io.ReadCloser) (err error) {
	defer rc.Close()

	targetTemp := targetFile + ".tmp"

	f, err := os.Create(targetTemp)
	if err != nil {
		return
	}

	_, err = io.Copy(f, rc)
	if err != nil {
		_ = f.Close()
		_ = os.Remove(targetTemp)

		return
	}

	err = f.Close()
	if err != nil {
		return
	}

	return os.Rename(targetTemp, targetFile)
}
