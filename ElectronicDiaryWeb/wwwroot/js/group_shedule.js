// Функция для загрузки стандартной формы расписания
function loadStandardScheduleForm() {
    const groupId = @(Model.SelectedGroupId ?? 0);
    if (!groupId) {
        alert('Пожалуйста, сначала выберите группу');
        return;
    }

    fetch(`/GroupSchedule/CreateStandardForm?groupId=${groupId}`)
        .then(response => {
            if (!response.ok) throw new Error('Ошибка загрузки формы');
            return response.text();
        })
        .then(html => {
            document.getElementById('standardLessonModalContent').innerHTML = html;
        })
        .catch(error => {
            console.error(error);
            document.getElementById('standardLessonModalContent').innerHTML =
                `<div class="alert alert-danger">Ошибка загрузки формы: ${error.message}</div>`;
        });
}

// Submit handlers
async function submitStandardForm(event) {
    event.preventDefault();
    const formData = new FormData(event.target);

    try {
        const response = await fetch(event.target.action, {
            method: 'POST',
            body: formData
        });

        if (response.ok) {
            $('#standardLessonModal').modal('hide');
            window.location.reload();
        } else {
            const errorText = await response.text();
            document.getElementById('standardLessonModalContent').innerHTML = errorText;
        }
    } catch (error) {
        console.error('Ошибка:', error);
    }
}

async function submitChangeForm(event) {
    event.preventDefault();
    const form = event.target;
    const formData = new FormData(form);

    // Конвертация FormData в объект с правильными форматами дат
    const data = {
        GroupId: formData.get('GroupId'),
        ChangeType: formData.get('ChangeType'),
        OldDate: formData.get('OldDate') || null,
        NewDate: formData.get('NewDate') || null,
        NewStartTime: formData.get('NewStartTime') || null,
        NewEndTime: formData.get('NewEndTime') || null,
        NewClassroom: formData.get('NewClassroom') || null,
        StandardScheduleId: formData.get('StandardScheduleId') ? parseInt(formData.get('StandardScheduleId')) : null
    };

    // Валидация
    const errors = validateChangeForm(data);
    if (errors.length > 0) {
        alert(`Ошибки:\n${errors.join("\n")}`);
        return;
    }

    try {
        const response = await fetch(`${apiBaseUrl}ScheduleChanges`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(data)
        });

        if (response.ok) {
            $('#scheduleChangeModal').modal('hide');
            window.location.reload();
        } else {
            const errorText = await response.text();
            alert(`Ошибка: ${errorText}`);
        }
    } catch (error) {
        console.error('Ошибка:', error);
        alert('Ошибка сети. Проверьте подключение.');
    }
}

function validateChangeForm(data) {
    const errors = [];

    if (!data.ChangeType) errors.push("Выберите тип изменения");

    if (data.ChangeType === "перенос") {
        if (!data.OldDate) errors.push("Укажите исходную дату");
        if (!data.StandardScheduleId) errors.push("Выберите время занятия");
        if (!data.NewDate) errors.push("Укажите новую дату");
    }

    if (data.ChangeType === "дополнительное") {
        if (!data.NewDate) errors.push("Укажите дату занятия");
        if (!data.NewStartTime) errors.push("Укажите время начала");
        if (!data.NewEndTime) errors.push("Укажите время окончания");
    }

    if (data.NewStartTime && data.NewEndTime && data.NewStartTime >= data.NewEndTime) {
        errors.push("Время окончания должно быть позже начала");
    }

    return errors;
}

// Load schedule change form
function loadScheduleChangeForm() {
    const groupId = @Model.SelectedGroupId;
    const container = document.getElementById('scheduleChangeModalContent');
    container.innerHTML = '<div class="text-center py-4"><i class="bi bi-arrow-repeat spinner"></i> Загрузка...</div>';

    fetch(`/GroupSchedule/CreateChangeForm?groupId=${groupId}`)
        .then(response => {
            if (!response.ok) throw new Error('Ошибка загрузки формы');
            return response.text();
        })
        .then(html => {
            container.innerHTML = html;
            // Инициализация скриптов после загрузки
            const scripts = container.querySelectorAll('script');
            scripts.forEach(script => {
                const newScript = document.createElement('script');
                newScript.text = script.text;
                document.body.appendChild(newScript).parentNode.removeChild(newScript);
            });
        })
        .catch(error => {
            console.error('Ошибка:', error);
            container.innerHTML = `
                    <div class="alert alert-danger">
                        <i class="bi bi-exclamation-triangle"></i>
                        Ошибка загрузки формы: ${error.message}
                    </div>
                `;
        });
}

let currentStandardId = null;
let currentChangeId = null;

function setDeleteParams(standardId, changeId) {
    currentStandardId = standardId !== 0 ? standardId : null;
    currentChangeId = changeId !== 0 ? changeId : null;

    const modalBody = document.getElementById('deleteModalBody');
    const confirmBtn = document.getElementById('confirmDeleteBtn');

    modalBody.innerHTML = '';

    if (currentStandardId && currentChangeId) {
        modalBody.innerHTML = `
                <div class="mb-3">
                    <p>Что именно вы хотите удалить?</p>
                    <div class="form-check">
                        <input class="form-check-input" type="radio" name="deleteOption"
                               id="deleteStandard" value="standard" checked>
                        <label class="form-check-label" for="deleteStandard">
                            Стандартное расписание (ID: ${currentStandardId})
                        </label>
                    </div>
                    <div class="form-check">
                        <input class="form-check-input" type="radio" name="deleteOption"
                               id="deleteChange" value="change">
                        <label class="form-check-label" for="deleteChange">
                            Изменение расписания (ID: ${currentChangeId})
                        </label>
                    </div>
                </div>
            `;
    } else if (currentStandardId) {
        modalBody.innerHTML = `
                <p>Вы уверены, что хотите удалить стандартное расписание (ID: ${currentStandardId})?</p>
            `;
    } else {
        modalBody.innerHTML = `
                <p>Вы уверены, что хотите удалить изменение расписания (ID: ${currentChangeId})?</p>
            `;
    }

    // Обработчик для кнопки удаления в основном модальном окне
    confirmBtn.onclick = function () {
        if (currentStandardId && !currentChangeId) {
            // Для стандартного расписания - показываем подтверждение
            $('#deleteModal').modal('hide');
            $('#confirmDeleteWithChangesModal').modal('show');
        } else {
            // Для изменений или при выборе - удаляем сразу
            performDelete();
        }
    };
}

// Обработчик для кнопки подтверждения удаления с изменениями
document.getElementById('confirmDeleteWithChanges')?.addEventListener('click', function () {
    $('#confirmDeleteWithChangesModal').modal('hide');
    performDelete();
});

async function performDelete() {
    let deleteType, recordId;

    if (currentStandardId && currentChangeId) {
        const selected = document.querySelector('input[name="deleteOption"]:checked').value;
        deleteType = selected;
        recordId = selected === 'standard' ? currentStandardId : currentChangeId;
    } else {
        deleteType = currentStandardId ? 'standard' : 'change';
        recordId = currentStandardId || currentChangeId;
    }

    const url = `${apiBaseUrl}${deleteType === 'standard' ? 'StandardSchedules' : 'ScheduleChanges'}/${recordId}`;

    try {
        const response = await fetch(url, {
            method: 'DELETE',
            headers: {
                'Accept': 'application/json'
            }
        });

        if (response.ok) {
            window.location.reload();
        } else {
            const errorData = await response.json();
            showErrorToast(errorData.error || 'Неизвестная ошибка');
        }
    } catch (error) {
        console.error('Ошибка:', error);
        showErrorToast('Ошибка сети');
    }
}

function showErrorToast(message) {
    alert(`Ошибка: ${message}`);
}