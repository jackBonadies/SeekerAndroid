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
	)
	flag.Parse()

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

	err = os.MkdirAll(*repoDir, 0o644)
	if err != nil {
		log.Fatalf("creating repo directory: %s\n", err.Error())
	}

	// map[apkName]info
	var apkInfoMap = make(map[string]apps.AppInfo)

	for _, app := range appsList {
		log.SetPrefix(fmt.Sprintf("%s/%s", app.Author(), app.Name()))

		repo, err := apps.RepoInfo(app.GitURL)
		if err != nil {
			log.Printf("Error while getting repo info from URL %q: %s", app.GitURL, err.Error())
			haveError = true
			continue
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
			continue
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

			apkInfoMap[appName] = app

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
	}
	log.SetPrefix("")

	// Now, we run the fdroid update command
	cmd := exec.Command("fdroid", "update", "-c")
	cmd.Stderr = os.Stderr
	cmd.Stdout = os.Stdout
	cmd.Stdin = os.Stdin
	cmd.Dir = filepath.Dir(*repoDir)

	err = cmd.Run()
	if err != nil {
		log.Println("Error while running \"fdroid update -c\":", err.Error())
		os.Exit(1)
	}

	indexFilePath := filepath.Join(*repoDir, "index-v1.json")

	fdroidIndex, err := apps.ReadIndex(indexFilePath)
	if err != nil {
		log.Fatalf("reading f-droid repo index: %s\n", err.Error())
	}

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
		delete(meta, "CurrentVersionCode")

		setIfEmpty(meta, "AuthorName", apkInfo.Author())
		setIfEmpty(meta, "Name", apkInfo.Name())
		setIfEmpty(meta, "SourceCode", apkInfo.GitURL)
		setIfEmpty(meta, "License", apkInfo.License)

		var summary = apkInfo.Summary
		// See https://f-droid.org/en/docs/Build_Metadata_Reference/#Summary for max length
		const maxSummaryLength = 80
		if len(summary) > maxSummaryLength {
			summary = summary[:maxSummaryLength-3] + "..."
		}

		setIfEmpty(meta, "Summary", summary)

		meta["CurrentVersion"] = latestPackage.VersionName

		err = apps.WriteMetaFile(path, meta)
		if err != nil {
			log.Printf("Writing meta file %q: %s", path, err.Error())
			return nil
		}

		return nil
	})
	if err != nil {
		log.Printf("Error while walking metadata: %s", err.Error())
		os.Exit(1)
	}

	fdroidIndex.RemoveVersionCode()

	err = apps.WriteIndex(indexFilePath, fdroidIndex)
	if err != nil {
		log.Printf("Writing back repo index: %s", err.Error())
	}

	// Now, we run the fdroid update command again to regenerate the index with our new metadata
	cmd = exec.Command("fdroid", "update")
	cmd.Stderr = os.Stderr
	cmd.Stdout = os.Stdout
	cmd.Stdin = os.Stdin
	cmd.Dir = filepath.Dir(*repoDir)

	err = cmd.Run()
	if err != nil {
		log.Println("Error while running \"fdroid update -c\":", err.Error())
		os.Exit(1)
	}

	if haveError {
		os.Exit(1)
	}
}

func setIfEmpty(m map[string]interface{}, key string, value string) {
	if m[key] == nil || m[key] == "" || m[key] == "Unknown" {
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
