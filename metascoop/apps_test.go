package main

import (
	"testing"

	"metascoop/apps"
)

func TestAppsFile(t *testing.T) {
	appsList, err := apps.ParseAppFile("../apps.yaml")
	if err != nil {
		t.Errorf("error parsing apps file: %s", err.Error())
	}

	if len(appsList) == 0 {
		t.Errorf("the app list is empty, wanted at least one app")
	}
}
