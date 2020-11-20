#!/bin/sh

GREP_OPTIONS=''

cookiejar=$(mktemp cookies.XXXXXXXXXX)
netrc=$(mktemp netrc.XXXXXXXXXX)
chmod 0600 "$cookiejar" "$netrc"
function finish {
  rm -rf "$cookiejar" "$netrc"
}

trap finish EXIT
WGETRC="$wgetrc"

prompt_credentials() {
    echo "Enter your Earthdata Login or other provider supplied credentials"
    read -p "Username (h_lev1): " username
    username=${username:-h_lev1}
    read -s -p "Password: " password
    echo "machine urs.earthdata.nasa.gov login $username password $password" >> $netrc
    echo
}

exit_with_error() {
    echo
    echo "Unable to Retrieve Data"
    echo
    echo $1
    echo
    echo "https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N34W121.zip"
    echo
    exit 1
}

prompt_credentials
  detect_app_approval() {
    approved=`curl -s -b "$cookiejar" -c "$cookiejar" -L --max-redirs 2 --netrc-file "$netrc" https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N34W121.zip -w %{http_code} | tail  -1`
    if [ "$approved" -ne "302" ]; then
        # User didn't approve the app. Direct users to approve the app in URS
        exit_with_error "Please ensure that you have authorized the remote application by visiting the link below "
    fi
}

setup_auth_curl() {
    # Firstly, check if it require URS authentication
    status=$(curl -s -z "$(date)" -w %{http_code} https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N34W121.zip | tail -1)
    if [[ "$status" -ne "200" && "$status" -ne "304" ]]; then
        # URS authentication is required. Now further check if the application/remote service is approved.
        detect_app_approval
    fi
}

setup_auth_wget() {
    # The safest way to auth via curl is netrc. Note: there's no checking or feedback
    # if login is unsuccessful
    touch ~/.netrc
    chmod 0600 ~/.netrc
    credentials=$(grep 'machine urs.earthdata.nasa.gov' ~/.netrc)
    if [ -z "$credentials" ]; then
        cat "$netrc" >> ~/.netrc
    fi
}

fetch_urls() {
  if command -v curl >/dev/null 2>&1; then
      setup_auth_curl
      while read -r line; do
        # Get everything after the last '/'
        filename="${line##*/}"

        # Strip everything after '?'
        stripped_query_params="${filename%%\?*}"

        curl -f -b "$cookiejar" -c "$cookiejar" -L --netrc-file "$netrc" -g -o $stripped_query_params -- $line && echo || exit_with_error "Command failed with error. Please retrieve the data manually."
      done;
  elif command -v wget >/dev/null 2>&1; then
      # We can't use wget to poke provider server to get info whether or not URS was integrated without download at least one of the files.
      echo
      echo "WARNING: Can't find curl, use wget instead."
      echo "WARNING: Script may not correctly identify Earthdata Login integrations."
      echo
      setup_auth_wget
      while read -r line; do
        # Get everything after the last '/'
        filename="${line##*/}"

        # Strip everything after '?'
        stripped_query_params="${filename%%\?*}"

        wget --load-cookies "$cookiejar" --save-cookies "$cookiejar" --output-document $stripped_query_params --keep-session-cookies -- $line && echo || exit_with_error "Command failed with error. Please retrieve the data manually."
      done;
  else
      exit_with_error "Error: Could not find a command-line downloader.  Please install curl or wget"
  fi
}

fetch_urls <<'EDSCEOF'
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N34W121.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N38W124.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N42W124.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N37W123.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N35W122.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N39W123.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N36W122.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N36W123.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N38W122.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N42W121.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N39W122.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N39W121.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N36W120.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N35W120.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N42W122.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N34W120.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N42W123.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N39W125.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N41W124.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N40W125.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N40W122.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N33W120.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N36W121.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N33W121.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N41W123.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N35W121.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N40W124.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N40W123.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N40W121.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N42W125.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N41W125.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N41W121.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N38W121.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N41W122.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N38W123.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N37W122.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N39W124.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N37W120.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N37W124.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N37W121.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N38W120.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N33W119.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N33W118.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N32W115.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N32W116.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N35W119.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N34W119.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N36W116.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N33W115.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N37W118.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N33W116.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N32W118.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N34W118.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N36W119.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N33W117.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N35W118.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N34W116.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N32W119.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N32W117.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N35W117.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N36W117.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N36W118.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N34W115.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N35W116.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N35W115.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N38W119.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N37W119.zip
https://e4ftl01.cr.usgs.gov//ASTER_B/ASTT/ASTGTM.003/2000.03.01/ASTGTMV003_N34W117.zip
EDSCEOF