# Contributing

When contributing to this repository, please first discuss the change you wish to make via issue,
email, or any other method with the owners of this repository before making a change. 

We don't maintain a Contributor License Agreement (CLA) but we do require that anyone that wishes to contribute agrees to the following:

* You have the right to assign the copyright of your contribution.
* By making your contribution, you are assigning copyright of your contribution to the maintainers of this repository.

In layman's terms, make sure that anything you contribute is yours to give, and understand that when you give it to us we own the legal rights to it.

## Contribution Workflow

1. Assign yourself to the issue that you'll be working on.  Move the issue into the 'In progress' column on the project
   board, if you have the necessary access.  If there's no issue associated with the work you'd like to do, your pull request is likely
   to be rejected until one is created and prioritized.  
2. Clone the repository and `git checkout develop` to ensure you are on the development branch.
3. Create a new branch for your change with `git checkout -b <your-branch-name>` be descriptive, but terse.
4. Make your changes.  When finished, push your branch with `git push origin --set-upstream <your-branch-name>`.
5. Create a pull request to merge `<your-branch-name>` into `develop`.  Pull requests to `master` are only accepted from
   maintainers.
6. A maintainer will review your pull request and may make comments, ask questions, or request changes.  When all
   feedback has been addressed the pull request will be approved, and after all checks have passed it will be merged by
   a maintainer, or you may merge it yourself if you have the necessary access.
7. Delete your branch, unless you plan to submit additional pull request from it.

Note that we require that all branches are up to date with target branch prior to merging.  If you see a message about this
on your pull request, use `git fetch` to retrieve the latest changes,  `git rebase origin/develop` to rebase your 
branch onto `develop`, and finally `git push origin <your-branch-name> -f` to push your updated branch to the repository.