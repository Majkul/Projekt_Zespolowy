(function () {
    const maxFiles = 5;
    const maxFileSizeBytes = 5 * 1024 * 1024;
    const allowedTypes = ['image/jpeg', 'image/png'];

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('input[type="file"].image-upload-input').forEach(initializeUploader);
    });

    function initializeUploader(input) {
        if (input.dataset.imageUploaderInitialized === 'true') {
            return;
        }

        input.dataset.imageUploaderInitialized = 'true';
        input.style.display = 'none';

        const dropzone = document.createElement('div');
        dropzone.className = 'image-dropzone border rounded p-4 text-center text-muted';
        dropzone.style.cssText = 'border-style:dashed!important;cursor:pointer;';
        dropzone.innerHTML =
            '<i class="bi bi-cloud-upload fs-3 d-block mb-1"></i>' +
            'Przeciągnij zdjęcia lub ' +
            '<span class="text-primary fw-medium">kliknij tutaj</span>' +
            '<div class="small mt-1">JPG, PNG · maks. 5 MB · do 5 zdjęć</div>';

        const previewGrid = document.createElement('div');
        previewGrid.className = 'd-flex flex-wrap gap-2 mt-2';

        const counter = document.createElement('div');
        counter.className = 'small text-muted mt-1';

        const errorBox = document.createElement('div');
        errorBox.className = 'text-danger small mt-2';

        input.parentNode.insertBefore(dropzone, input);
        input.insertAdjacentElement('afterend', errorBox);
        errorBox.insertAdjacentElement('afterend', previewGrid);
        previewGrid.insertAdjacentElement('afterend', counter);

        let selectedFiles = Array.from(input.files || []);
        let objectUrls = [];
        let suppressChange = false;

        dropzone.addEventListener('click', function () {
            input.click();
        });

        dropzone.addEventListener('dragover', function (event) {
            event.preventDefault();
            dropzone.classList.add('bg-primary', 'bg-opacity-10', 'border-primary');
        });

        dropzone.addEventListener('dragleave', function () {
            dropzone.classList.remove('bg-primary', 'bg-opacity-10', 'border-primary');
        });

        dropzone.addEventListener('drop', function (event) {
            event.preventDefault();
            dropzone.classList.remove('bg-primary', 'bg-opacity-10', 'border-primary');
            handleFiles(Array.from(event.dataTransfer.files || []), true);
        });

        input.addEventListener('change', function () {
            if (suppressChange) {
                return;
            }

            handleFiles(Array.from(input.files || []), true);
        });

        render();

        function handleFiles(files, notifyChange) {
            const validation = validateFiles(files);
            selectedFiles = validation.validFiles;
            syncInputFiles(notifyChange);
            render(validation.messages);
        }

        function validateFiles(files) {
            const validFiles = [];
            const messages = [];

            files.forEach(function (file) {
                if (validFiles.length >= maxFiles) {
                    messages.push('Pominięto nadmiarowy plik "' + file.name + '". Możesz dodać maksymalnie 5 zdjęć.');
                    return;
                }

                if (!allowedTypes.includes(file.type)) {
                    messages.push('Plik "' + file.name + '" ma nieobsługiwany format. Dozwolone są JPG i PNG.');
                    return;
                }

                if (file.size > maxFileSizeBytes) {
                    messages.push('Plik "' + file.name + '" przekracza maksymalny rozmiar 5 MB.');
                    return;
                }

                validFiles.push(file);
            });

            return { validFiles: validFiles, messages: messages };
        }

        function syncInputFiles(notifyChange) {
            const dataTransfer = new DataTransfer();
            selectedFiles.forEach(function (file) {
                dataTransfer.items.add(file);
            });

            suppressChange = true;
            input.files = dataTransfer.files;

            if (notifyChange) {
                input.dispatchEvent(new Event('change', { bubbles: true }));
            }

            suppressChange = false;
        }

        function render(messages) {
            revokeObjectUrls();
            previewGrid.innerHTML = '';
            errorBox.innerHTML = '';

            (messages || []).forEach(function (message) {
                const messageElement = document.createElement('div');
                messageElement.textContent = message;
                errorBox.appendChild(messageElement);
            });

            selectedFiles.forEach(function (file, index) {
                const wrapper = document.createElement('div');
                wrapper.className = 'position-relative';

                const image = document.createElement('img');
                const objectUrl = URL.createObjectURL(file);
                objectUrls.push(objectUrl);
                image.src = objectUrl;
                image.alt = file.name;
                image.className = 'rounded border';
                image.style.cssText = 'width:80px;height:80px;object-fit:cover;';

                const removeButton = document.createElement('button');
                removeButton.type = 'button';
                removeButton.className = 'btn btn-danger btn-sm rounded-circle position-absolute top-0 end-0 translate-middle';
                removeButton.style.cssText = 'width:24px;height:24px;line-height:1;padding:0;';
                removeButton.setAttribute('aria-label', 'Usuń zdjęcie');
                removeButton.innerHTML = '&times;';
                removeButton.addEventListener('click', function () {
                    selectedFiles.splice(index, 1);
                    syncInputFiles(true);
                    render();
                });

                wrapper.appendChild(image);
                wrapper.appendChild(removeButton);
                previewGrid.appendChild(wrapper);
            });

            counter.textContent = selectedFiles.length + '/' + maxFiles + ' zdjęć';
        }

        function revokeObjectUrls() {
            objectUrls.forEach(function (objectUrl) {
                URL.revokeObjectURL(objectUrl);
            });
            objectUrls = [];
        }
    }
})();
