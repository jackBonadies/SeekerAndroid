package apps

import (
	"context"
	"fmt"
	"strings"
	"unicode"

	"github.com/google/go-github/v39/github"
	"golang.org/x/text/runes"
	"golang.org/x/text/transform"
	"golang.org/x/text/unicode/norm"
)

func FindAPKRelease(release *github.RepositoryRelease) *github.ReleaseAsset {
	for _, asset := range release.Assets {
		if asset.State == nil || *asset.State != "uploaded" {
			continue
		}

		if asset.Name != nil && strings.HasSuffix(*asset.Name, ".apk") {
			return asset
		}
	}

	return nil
}

func GenerateReleaseFilename(appName string, tagName string) string {
	var normalName = fmt.Sprintf("%s_%s.apk", appName, tagName)

	var tc = transform.Chain(norm.NFD, runes.Remove(runes.Predicate(func(r rune) bool {
		return unicode.Is(unicode.Mn, r)
	})), norm.NFC)

	cleaned, _, err := transform.String(tc, normalName)
	if err != nil {
		cleaned = normalName
	}

	return strings.Map(func(r rune) rune {
		if unicode.IsSpace(r) {
			return '_'
		}
		if r >= 'a' && r <= 'z' || r >= 'A' && r <= 'Z' || r >= '0' && r <= '9' {
			return r
		}
		if r == '_' || r == '-' || r == '.' {
			return r
		}
		return -1
	}, cleaned)
}

func ListAllReleases(githubClient *github.Client, appRepoAuthor, appRepoName string) (allReleases []*github.RepositoryRelease, err error) {
	var currentPage int = 1

	for {
		rels, _, ierr := githubClient.Repositories.ListReleases(context.Background(), appRepoAuthor, appRepoName, &github.ListOptions{
			Page:    currentPage,
			PerPage: 100,
		})
		if ierr != nil || len(rels) == 0 {
			err = ierr
			break
		}

		allReleases = append(allReleases, rels...)
		currentPage++
	}

	return
}
