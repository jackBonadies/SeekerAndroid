package apps

import (
	"os"

	"gopkg.in/yaml.v3"
)

func ReadMetaFile(path string) (d map[string]interface{}, err error) {
	f, err := os.Open(path)
	if err != nil {
		return
	}
	defer f.Close()

	err = yaml.NewDecoder(f).Decode(&d)

	return
}

func WriteMetaFile(path string, data map[string]interface{}) (err error) {
	tmpPath := path + ".tmp"
	f, err := os.Create(tmpPath)
	if err != nil {
		return
	}

	err = yaml.NewEncoder(f).Encode(data)
	if err != nil {
		_ = f.Close()
		return
	}

	err = f.Close()
	if err != nil {
		return
	}

	return os.Rename(tmpPath, path)
}
