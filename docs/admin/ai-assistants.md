# AI assistants

AI works with ChefBot in two ways, both under **Settings → AI assistants**:

- **AI models ChefBot uses** (admins): ChefBot itself asks a model you choose to
  [read recipes from photos](../using/recipes.md#from-a-photo) and to
  [suggest what a recipe is missing](../using/recipes.md#make-a-recipe-more-complete).
- **Assistants connected to ChefBot** (everyone): ChefBot is an [MCP](https://modelcontextprotocol.io) server, so AI
  assistants such as Claude can work with your kitchen. You can ask one to plan next week, swap Tuesday's dinner for
  something with chicken, mark tonight's meal cooked, or add milk to the shopping list.

## AI models ChefBot uses

This part is for admins. Nothing is sent to a model until someone in the household asks: by reading a photo, by
pressing **Ask for suggestions** on a recipe, or by starting the library sweep below.

### Set up a model

1. Open **Settings → AI assistants**.
2. Under **Start from**, pick one, and press **Add a model**. Each choice only fills in the fields:

   | Start from | Talks to | Fill in |
   |---|---|---|
   | **Claude (Anthropic)** | Anthropic's API | An **API key** from the Claude Console, under API keys. The model starts as `claude-opus-5-5` |
   | **OpenAI** | `https://api.openai.com/v1` | An **API key** and the **Model** |
   | **Ollama on your network** | `http://ollama.local:11434/v1` | The **Address** of your Ollama server, ending in `/v1`, and the **Model**. No key |
   | **Another OpenAI-compatible service** | Any address | The **Address**, ending in `/v1`, the **Model**, and a key if the service needs one |

   LM Studio, OpenRouter, vLLM and other services that answer OpenAI's chat completions API work as
   **OpenAI-compatible**. An address on your own network is fine here, unlike for recipe links.
3. Choose the **Default model**. Every task uses it unless you choose another.
4. Press **Save models**, then **Test** on the model. It asks a short question and says whether the model answered.

API keys are never shown again after saving. Leave the key field empty to keep the saved one.

> [!NOTE]
> Reading photos needs a model that can see images. Claude and OpenAI's current models can. With Ollama, pull a
> vision model such as `qwen2.5vl` or `llama3.2-vision`.

### Use a different model for a task

With two or more models, **Use a different model for…** lists each task:

- **Reading recipe photos**
- **Completing recipes**

Pick a model for one, or leave it on **The default model**. For example, read photos with a local model and complete
recipes with Claude. Removing a model sends its tasks back to the default.

### Complete sparse recipes

Once a model is set up, **Complete sparse recipes** counts the recipes missing details: no description, no times, no
tags, ingredient lines without amounts, or steps that are short or missing. **Ask for suggestions** queues them all.
ChefBot asks the model about one recipe at a time in the background, and suggestions appear on each recipe as they're
ready. Find them on the **Recipes** tab with the **Has suggestions** filter.

> [!WARNING]
> Each recipe is sent to the model, which costs money with a paid service. A recipe is asked about again only after
> it changes, even if its suggestions were set aside.

### Keys and backups

API keys are kept in ChefBot's database and in [backups](backups-and-restore.md), like to-do app tokens. Keep backups
somewhere private.

## Connect an assistant to ChefBot

### What an assistant can do

Search and import recipes, read the week's plan, plan a week, swap, move or mark meals cooked, and look at or change
the pantry and the shopping list. Admin settings (people, stores, plan styles, backups and so on) aren't offered.

### Connect one

1. Open **Settings → AI assistants**.
2. Under **Name for a new token**, enter the assistant it's for, and press **Make a token**.
3. Copy the token. It's shown only once.
4. Give the assistant the **MCP address**, `https://<ChefBot's address>:8443/mcp`, and the token, sent as the header
   `Authorization: Bearer <token>`.

#### Claude Code

ChefBot shows the exact command after making a token. It looks like this:

```sh
claude mcp add --transport http chefbot https://chefbot.local:8443/mcp --header "Authorization: Bearer chefbot_..."
```

#### Other assistants

Any client that supports MCP over Streamable HTTP with a custom header takes the same address and header.

### Tokens

- A token acts as the person who made it. Each person makes and revokes their own.
- It keeps working after a password change, and stops when it's revoked or its account is removed.
- **Revoke** a token on the same page when you stop using an assistant.
- Tokens aren't kept in backups, so after a restore, make new ones.
- Tokens only work on `/mcp`, not on the rest of the API.

### The assistant has to reach ChefBot

- An assistant running on your computer (such as Claude Code) can reach ChefBot on your home network.
- An assistant running in the cloud can't, unless ChefBot is reachable from the internet. See
  [Behind a reverse proxy](reverse-proxy.md).
- With ChefBot's own certificate, the machine running the assistant has to trust it. See
  [Trust the certificate](https-and-certificates.md#trust-the-certificate-on-your-devices).

## If something goes wrong

For photos and suggestions, see [Recipe import problems](../troubleshooting/recipe-imports.md#a-photo-wont-read). For
connected assistants, see [Notifications and integrations](../troubleshooting/notifications-and-integrations.md#ai-assistants).
