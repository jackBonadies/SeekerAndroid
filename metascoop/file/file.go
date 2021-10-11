package file

import (
	"io"
	"os"
)

// Move moves a file from oldpath to newpath
func Move(oldpath, newpath string) (err error) {
	// Try the normal method using rename
	err = os.Rename(oldpath, newpath)
	if err == nil {
		return
	}

	// This part is reached either on permission errors
	// OR when it's not possible to move files between drives (especially on windows).
	// To fix it, we copy the file without renaming it

	// Basically open the old file, create the new file and copy the contents.
	srcFile, err := os.Open(oldpath)
	if err != nil {
		return
	}

	destFile, err := os.Create(newpath)
	if err != nil {
		_ = srcFile.Close()
		return
	}

	_, err = io.Copy(destFile, srcFile)

	_ = srcFile.Close()

	if err != nil {
		// Clean up newly created file
		_ = destFile.Close()
		_ = os.Remove(newpath)
		return
	}

	err = destFile.Close()
	if err != nil {
		return
	}

	// Now that we know that the destination file was created, we can remove the old path
	return os.Remove(oldpath)
}
