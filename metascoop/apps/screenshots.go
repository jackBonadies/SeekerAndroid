package apps

import (
	"io/fs"
	"path/filepath"
	"strings"
)

type RepoMetadata struct {
	Screenshots []string
}

var imageSuffixes = map[string]bool{
	"png":  true,
	"jpg":  true,
	"jpeg": true,
}

func hasImageSuffix(path string) bool {
	return imageSuffixes[strings.TrimPrefix(filepath.Ext(path), ".")]
}

func FindMetadata(clonedRepoPath string) (r RepoMetadata, err error) {
	abs, err := filepath.Abs(clonedRepoPath)
	if err != nil {
		return
	}

	err = filepath.WalkDir(abs, func(path string, d fs.DirEntry, err error) error {
		if err != nil || d.IsDir() {
			return err
		}

		lp := strings.ToLower(path)

		if strings.Contains(lp, "screenshot") && hasImageSuffix(path) {
			r.Screenshots = append(r.Screenshots, path)
			return nil
		}

		return nil
	})

	return
}
