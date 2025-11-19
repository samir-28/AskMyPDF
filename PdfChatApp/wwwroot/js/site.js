


const uploadArea = document.getElementById('uploadArea');
const pdfFileInput = document.getElementById('pdfFileInput');
const browseBtn = document.getElementById('browseBtn');
const uploadSection = document.getElementById('uploadSection');
const uploadProgress = document.getElementById('uploadProgress');
const documentInfo = document.getElementById('documentInfo');
const chatMessages = document.getElementById('chatMessages');
const chatForm = document.getElementById('chatForm');
const messageInput = document.getElementById('messageInput');
const sendBtn = document.getElementById('sendBtn');
const sessionIdInput = document.getElementById('sessionId');
const newDocumentBtn = document.getElementById('newDocumentBtn');
const sidebarToggle = document.getElementById('sidebarToggle');
const sidebar = document.getElementById('sidebar');

let currentSessionId = sessionIdInput.value || '';


document.addEventListener('DOMContentLoaded', () => {
    setupEventListeners();
    autoResizeTextarea();
});

function setupEventListeners() {
    
    browseBtn?.addEventListener('click', () => pdfFileInput?.click());
    pdfFileInput?.addEventListener('change', handleFileSelect);

  
    uploadArea?.addEventListener('dragover', handleDragOver);
    uploadArea?.addEventListener('dragleave', handleDragLeave);
    uploadArea?.addEventListener('drop', handleDrop);


    chatForm?.addEventListener('submit', handleChatSubmit);
    messageInput?.addEventListener('input', autoResizeTextarea);
    messageInput?.addEventListener('keydown', handleKeyDown);

  
    newDocumentBtn?.addEventListener('click', resetSession);


    sidebarToggle?.addEventListener('click', toggleSidebar);
}


function handleDragOver(e) {
    e.preventDefault();
    e.stopPropagation();
    uploadArea?.classList.add('drag-over');
}

function handleDragLeave(e) {
    e.preventDefault();
    e.stopPropagation();
    uploadArea?.classList.remove('drag-over');
}

function handleDrop(e) {
    e.preventDefault();
    e.stopPropagation();
    uploadArea?.classList.remove('drag-over');

    const files = e.dataTransfer?.files;
    if (files && files.length > 0) {
        handleFileUpload(files[0]);
    }
}

function handleFileSelect(e) {
    const files = e.target?.files;
    if (files && files.length > 0) {
        handleFileUpload(files[0]);
    }
}

async function handleFileUpload(file) {
    if (!file) return;

    if (!file.name.toLowerCase().endsWith('.pdf')) {
        showToast('error', 'Invalid File', 'Please select a PDF file.');
        return;
    }

    if (file.size > 50 * 1024 * 1024) {
        showToast('error', 'File Too Large', 'Maximum file size is 50MB.');
        return;
    }

   
    if (uploadSection) uploadSection.style.display = 'none';
    if (uploadProgress) uploadProgress.style.display = 'block';

    const formData = new FormData();
    formData.append('pdfFile', file);

    try {
        const response = await fetch('/Chat/Upload', {
            method: 'POST',
            body: formData
        });

        const result = await response.json();

        if (result.success) {
            currentSessionId = result.sessionId;
            sessionIdInput.value = result.sessionId;

            
            displayDocumentInfo(result.fileName, result.pageCount);
            enableChat();
            clearWelcomeMessage();

            showToast('success', 'PDF Uploaded', result.message);
        } else {
            throw new Error(result.error || 'Upload failed');
        }
    } catch (error) {
        console.error('Upload error:', error);
        showToast('error', 'Upload Failed', error.message);

        
        if (uploadProgress) uploadProgress.style.display = 'none';
        if (uploadSection) uploadSection.style.display = 'block';
    }
}

function displayDocumentInfo(fileName, pageCount) {
    if (uploadSection) uploadSection.style.display = 'none';
    if (uploadProgress) uploadProgress.style.display = 'none';
    if (documentInfo) documentInfo.style.display = 'block';

    const fileNameEl = document.getElementById('fileName');
    const uploadTimeEl = document.getElementById('uploadTime');

    if (fileNameEl) fileNameEl.textContent = fileName;
    if (uploadTimeEl) uploadTimeEl.textContent = new Date().toLocaleString();
}

function enableChat() {
    if (messageInput) {
        messageInput.disabled = false;
        messageInput.focus();
    }
    if (sendBtn) sendBtn.disabled = false;
}

function clearWelcomeMessage() {
    const welcomeMsg = chatMessages?.querySelector('.welcome-message');
    if (welcomeMsg) welcomeMsg.remove();
}


async function handleChatSubmit(e) {
    e.preventDefault();

    const message = messageInput?.value.trim();
    if (!message || !currentSessionId) return;

    
    addMessage('user', message);

    
    if (messageInput) {
        messageInput.value = '';
        messageInput.style.height = 'auto';
    }

    
    showTypingIndicator();

    try {
        const response = await fetch('/Chat/Ask', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                message: message,
                sessionId: currentSessionId
            })
        });

        const result = await response.json();

        
        removeTypingIndicator();

        if (result.success) {
            addMessage('assistant', result.message);
        } else {
            throw new Error(result.error || 'Failed to get response');
        }
    } catch (error) {
        console.error('Chat error:', error);
        removeTypingIndicator();
        showToast('error', 'Error', error.message);
        addMessage('assistant', 'I encountered an error. Please try again.');
    }
}

function addMessage(role, content) {
    const messageDiv = document.createElement('div');
    messageDiv.className = `message ${role}`;

    const avatar = document.createElement('div');
    avatar.className = 'message-avatar';
    avatar.innerHTML = role === 'user' ? '<i class="fas fa-user"></i>' : '<i class="fas fa-robot"></i>';

    const messageContent = document.createElement('div');
    messageContent.className = 'message-content';

    const text = document.createElement('div');
    text.className = 'message-text';

    
    if (role === 'assistant') {
        text.innerHTML = formatMessageContent(content);
    } else {
        text.textContent = content;
    }

    const time = document.createElement('div');
    time.className = 'message-time';
    time.textContent = new Date().toLocaleTimeString();

    messageContent.appendChild(text);
    messageContent.appendChild(time);

    messageDiv.appendChild(avatar);
    messageDiv.appendChild(messageContent);

    chatMessages?.appendChild(messageDiv);
    scrollToBottom();
}

function formatMessageContent(content) {
    
    let formatted = content;

    
    formatted = formatted.replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>');

   
    formatted = formatted.replace(/\*(.+?)\*/g, '<em>$1</em>');

    
    const lines = formatted.split('\n');
    let inList = false;
    let formattedLines = [];

    lines.forEach(line => {
        const trimmedLine = line.trim();

       
        if (trimmedLine.match(/^[•\-\*]\s+/)) {
            if (!inList) {
                formattedLines.push('<ul class="formatted-list">');
                inList = true;
            }
            const listItem = trimmedLine.replace(/^[•\-\*]\s+/, '');
            formattedLines.push(`<li>${listItem}</li>`);
        }
       
        else if (trimmedLine.match(/^\d+\.\s+/)) {
            if (!inList) {
                formattedLines.push('<ol class="formatted-list">');
                inList = true;
            }
            const listItem = trimmedLine.replace(/^\d+\.\s+/, '');
            formattedLines.push(`<li>${listItem}</li>`);
        }
        else {
            if (inList) {
               
                const lastListStart = formattedLines[formattedLines.length - 1];
                if (lastListStart && !lastListStart.includes('<li>')) {
                    formattedLines.push(lastListStart.includes('<ul') ? '</ul>' : '</ol>');
                    inList = false;
                }
            }

            
            if (trimmedLine) {
                formattedLines.push(`<p>${trimmedLine}</p>`);
            } else if (formattedLines.length > 0) {
                formattedLines.push('<br>');
            }
        }
    });

    
    if (inList) {
        const hasUL = formattedLines.some(line => line.includes('<ul'));
        formattedLines.push(hasUL ? '</ul>' : '</ol>');
    }

    formatted = formattedLines.join('');

   
    formatted = formatted.replace(/`(.+?)`/g, '<code>$1</code>');

 
    return formatted;
}

function showTypingIndicator() {
    const indicator = document.createElement('div');
    indicator.className = 'message assistant typing';
    indicator.id = 'typingIndicator';

    const avatar = document.createElement('div');
    avatar.className = 'message-avatar';
    avatar.innerHTML = '<i class="fas fa-robot"></i>';

    const messageContent = document.createElement('div');
    messageContent.className = 'message-content';

    const typingDiv = document.createElement('div');
    typingDiv.className = 'typing-indicator';
    typingDiv.innerHTML = '<span></span><span></span><span></span>';

    messageContent.appendChild(typingDiv);
    indicator.appendChild(avatar);
    indicator.appendChild(messageContent);

    chatMessages?.appendChild(indicator);
    scrollToBottom();
}

function removeTypingIndicator() {
    const indicator = document.getElementById('typingIndicator');
    indicator?.remove();
}

function scrollToBottom() {
    if (chatMessages) {
        chatMessages.scrollTop = chatMessages.scrollHeight;
    }
}


function autoResizeTextarea() {
    if (messageInput) {
        messageInput.style.height = 'auto';
        messageInput.style.height = Math.min(messageInput.scrollHeight, 120) + 'px';
    }
}

function handleKeyDown(e) {
    if (e.key === 'Enter' && !e.shiftKey) {
        e.preventDefault();
        chatForm?.dispatchEvent(new Event('submit'));
    }
}


function showToast(type, title, message) {
    const toastContainer = document.getElementById('toastContainer');
    if (!toastContainer) return;

    const toast = document.createElement('div');
    toast.className = `toast ${type}`;

    const iconMap = {
        success: 'fa-check-circle',
        error: 'fa-exclamation-circle',
        warning: 'fa-exclamation-triangle'
    };

    toast.innerHTML = `
        <div class="toast-icon">
            <i class="fas ${iconMap[type] || 'fa-info-circle'}"></i>
        </div>
        <div class="toast-content">
            <div class="toast-title">${title}</div>
            <div class="toast-message">${message}</div>
        </div>
    `;

    toastContainer.appendChild(toast);

    setTimeout(() => {
        toast.style.animation = 'slideInRight 0.3s ease reverse';
        setTimeout(() => toast.remove(), 300);
    }, 4000);
}

// Session Management
async function resetSession() {
    if (confirm('Do you want to remove the PDF ?')) {
        try {
            await fetch('/Chat/ClearSession', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ sessionId: currentSessionId })
            });

            
            currentSessionId = '';
            sessionIdInput.value = '';
            chatMessages.innerHTML = `
                <div class="welcome-message">
                    <i class="fas fa-robot"></i>
                    <h3>Welcome to AskMyPDF!</h3>
                    <p>Upload your PDF document to start asking questions.</p>
                </div>
            `;

            if (documentInfo) documentInfo.style.display = 'none';
            if (uploadSection) uploadSection.style.display = 'block';
            if (messageInput) messageInput.disabled = true;
            if (sendBtn) sendBtn.disabled = true;
            if (pdfFileInput) pdfFileInput.value = '';

            showToast('success', 'PDF removed', 'You can upload a new PDF now.');
        } catch (error) {
            console.error('Reset error:', error);
            showToast('error', 'Reset Failed', 'Could not reset session.');
        }
    }
}

function restoreSession(sessionId, fileName) {
    currentSessionId = sessionId;
    displayDocumentInfo(fileName, 0);
    enableChat();
    clearWelcomeMessage();
}


function toggleSidebar() {
    sidebar?.classList.toggle('active');
}


window.restoreSession = restoreSession;