# PDP Website

## Setup process
### Windows
Recommended method for the setup is through WSL but docker is also an alternative if you know how to.

First install all required components for [WSL2](https://learn.microsoft.com/en-us/windows/wsl/install#prerequisites).

Next download atleast a Ubuntu 22.04 instance from Windows Store.

Start up the Ubuntu 22.04 instance after it has been downloaded and register your user and password for the container.

Once you are in after inital setup run the commands 
```
sudo apt-get update
sudo apt-get upgrade -y
sudo apt-get install postgresql-14 redis-server -y
```
#### Postgres
After all commands are successfully run enter the following command:
`sudo -u postgres psql`
<br />
Now you should be inside of the postgres-cli interface from here you should run: 
`\password postgres`
and then enter the new password as `postgres`
<br />
This is required in order for the backend to run.
You can now exit postgres-cli with `\q`

#### Redis
Next run `redis-cli` to configure a redis user
<br />
You should now be in the redis-cli interface from here you should run the following commands
```
ACL SETUSER redis on >redis ~* &* +@all
CONFIG REWRITE
```
You should now have a redis user and can safely exit the cli with hitting `ctrl + c`

#### systemd
By default systemd might not be enabled when setting up the ubuntu container for the first time to make sure it is active type the command: `sudo nano /etc/wsl.conf`
and then enter the following lines:
```
[boot]
systemd=true
```
Hit `CTRL + O` then `CTRL + X`

#### Final steps
Close down the ubuntu terminal window and open up a normal powershell window and run `wsl --shutdown`

You can now enter back into the ubuntu container again by typing `wsl` and systemd should be running
you can check this by `systemctl list-unit-files --type=service`

To enable the start of postgres and redis when entering wsl type the commands:
```
sudo systemctl enable postgresql
sudo systemctl enable redis-server
```
Now to start the services if their not currently running or stuck you type
```
sudo systemctl restart postgresql
sudo systemctl restart redis-server
```

## Important
The WSL terminal window has to be in the background *either minimized or behind another window* in order for postgres and redis to be running if either are not running the entire backend will not work correctly.