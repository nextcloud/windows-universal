@echo off

git diff master NextcloudApp\Strings > patch.tmp
git apply patch.tmp -R
del patch.tmp
git add NextcloudApp\Strings
git commit -m "[auto merge] updated from transifex"