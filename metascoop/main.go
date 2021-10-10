package main

import (
	"context"
	"errors"
	"flag"
	"fmt"
	"io"
	"log"
	"metascoop/apps"
	"net/http"
	"os"
	"path/filepath"
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

	for _, app := range appsList {
		log.SetPrefix(fmt.Sprintf("%s/%s", app.Author(), app.Name()))

		repo, err := apps.RepoInfo(app.GitURL)
		if err != nil {
			log.Printf("Error while getting repo info from URL %q: %s", app.GitURL, err.Error())
			haveError = true
			continue
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

	if haveError {
		os.Exit(1)
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
