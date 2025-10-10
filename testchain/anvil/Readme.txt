Run Anvil with Base Mainnet fork using:

Windows (Git Bash or WSL):
  ./start-anvil-base-fork.sh

Linux/Mac:
  ./start-anvil-base-fork.sh

The script will:
- Fork Base Mainnet from Infura
- Listen on http://127.0.0.1:8545
- Use default Anvil accounts

To clean Anvil state:
  rm -rf ~/.foundry/anvil/tmp/
