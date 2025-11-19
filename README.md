# AskMyPDF – AI-Powered PDF Chat Assistant (.NET 9 MVC)

<img src="https://github.com/samir-28/AskMyPDF/blob/master/Screenshot%202025-11-19%20184811.png" alt="Screenshot" width="600">

**AskMyPDF** is a tool that allows users to upload PDF documents and instantly ask questions about their content. It extracts the text from the file and uses that information to generate accurate answers with the help of **Ollama AI**.  

Everything runs fully offline using Ollama’s local AI models.

---

## Features

- Upload PDFs (up to 50MB)  
- Extract text using iText7  
- Ask questions about the PDF content  
- Session-based chat history  
- Responsive, mobile-friendly UI  
- Toast notifications for actions  

---

## Prerequisites

- .NET 9 SDK  
- Ollama installed locally  
- At least one AI model (e.g., llama3.2)  

---

## Installation

### 1. Install Ollama
Download from [Ollama](https://ollama.com) or use:

```bash
curl -fsSL https://ollama.com/install.sh | sh
2. Pull a model
bash
Copy code
ollama pull llama3.2
3. Start Ollama service
bash
Copy code
ollama serve
4. Create .NET project & Install dependencies
bash
Copy code
dotnet add package itext7
5. Configure Ollama (appsettings.json)
json
Copy code
{
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "ModelName": "llama3.2"
  }
}
6. Run the app
bash
Copy code
dotnet restore
dotnet build
dotnet run

```

## How it Works
-Upload a PDF

-Wait for text extraction

-Ask questions about the document

-View chat history per session

-Upload new PDFs to start new sessions

## Security Features
-PDF-only uploads

-50MB size limit

-No permanent storage

-Session timeout: 2 hours

## Note:
-This is a personal project done using a free Ollama model and .NET 9.
