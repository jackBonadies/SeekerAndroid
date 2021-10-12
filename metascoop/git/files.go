package git

import (
	"fmt"
	"os/exec"
	"strings"
)

func GetChangedFileNames(repoPath string) (paths []string, err error) {
	cmd := exec.Command("git", "diff", "--name-only")
	cmd.Dir = repoPath

	output, err := cmd.Output()
	if err != nil {
		err = fmt.Errorf("running git: %w\nOutput:\n%s", err, string(output))
		return
	}

	paths = strings.Split(strings.TrimSpace(string(output)), "\n")

	return
}
