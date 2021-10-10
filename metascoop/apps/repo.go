package apps

import (
	"net/url"
	"strings"
)

type Repo struct {
	Author string
	Name   string
	Host   string
}

func RepoInfo(repoURL string) (r Repo, err error) {
	u, uerr := url.ParseRequestURI(repoURL)
	if uerr != nil {
		return
	}

	split := strings.Split(strings.Trim(u.Path, "/"), "/")
	if len(split) < 2 {
		return
	}

	r.Author = split[0]
	r.Name = split[1]
	r.Host = strings.TrimPrefix(u.Host, "www.")

	return
}
