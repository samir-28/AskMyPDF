AskMyPDF – AI-Powered PDF Chat Assistant (.NET 9 MVC)

AskMyPDF is a tool that allows users to upload PDF documents and instantly ask questions about their content. It extracts the text from the file and uses that information to generate accurate answers with the help of Ollama AI.

Everything runs fully offline using Ollama’s local AI models.

Features

Upload PDFs (up to 50MB)

Extract text using iText7

Ask questions about the PDF content

Session-based chat history

Responsive, mobile-friendly UI

Toast notifications for actions

Prerequisites

.NET 9 SDK

Ollama installed locally

At least one AI model (e.g., llama3.2)

Installation

Install Ollama
Download from Ollama
 or use:

curl -fsSL https://ollama.com/install.sh | sh


Pull a model

ollama pull llama3.2


Start Ollama service

ollama serve

Create .NET project

Install dependencies

dotnet add package itext7


Configure Ollama (appsettings.json)

{
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "ModelName": "llama3.2"
  }
}


Run the app

dotnet restore
dotnet build
dotnet run

How it works ?

Upload a PDF

Wait for text extraction

Ask questions about the document

View chat history per session

Upload new PDFs to start new sessions



Security Features :

PDF-only uploads

50MB size limit

No permanent storage

Session timeout: 2 hours

Note :
Its personal project  done with free ollama model and .net 9 .
