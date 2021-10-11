#!/bin/bash

cd metascoop
go run main.go -ap=../apps.yaml -rd=../fdroid/repo -pat="$GH_ACCESS_TOKEN"
EXIT_CODE=$?
cd ..

set -e

if test $EXIT_CODE -eq 2; then
    # Exit code 2 means that there were no significant changes
    exit 0
elif test $EXIT_CODE -eq 0; then
    # Exit code 0 means that we can commit everything & push

    git config --global user.name 'github-actions'
    git config --global user.email '41898282+github-actions[bot]@users.noreply.github.com'

    git add fdroid
    git commit -m"Automated update"
    git push
else 
    exit $EXIT_CODE
fi
