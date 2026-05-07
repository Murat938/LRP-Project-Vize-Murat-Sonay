document.addEventListener('DOMContentLoaded', async () => {
    await requireRole('Student');
    setupSidebar();
    loadStudentOverview();
});

function setupSidebar() {
    document.querySelectorAll('.sidebar-nav a').forEach((link) => {
        link.addEventListener('click', (event) => {
            event.preventDefault();
            loadStudentOverview();
            document.querySelectorAll('.sidebar-nav a').forEach((item) => item.classList.remove('active'));
            event.target.classList.add('active');
        });
    });
}

async function loadStudentOverview() {
    const content = document.querySelector('#pageContent');
    try {
        const result = await apiFetch('/api/student/computer');
        content.innerHTML = `
            <div class="dashboard-grid mb-4">
                <div class="metric-card p-4">
                    <span class="metric-label">Öğrenci</span>
                    <div class="d-flex align-items-center justify-content-between">
                        <h3>${result.fullName}</h3>
                        <div class="metric-icon"><i class="fas fa-user-graduate"></i></div>
                    </div>
                    <p>${result.studentNumber} numaralı öğrenci.</p>
                </div>
                <div class="metric-card p-4">
                    <span class="metric-label">Bilgisayar Kodu</span>
                    <div class="d-flex align-items-center justify-content-between">
                        <h3>${result.computer.assetCode}</h3>
                        <div class="metric-icon"><i class="fas fa-barcode"></i></div>
                    </div>
                    <p>Zimmetlenmiş cihaz kodu.</p>
                </div>
                <div class="metric-card p-4">
                    <span class="metric-label">Lokasyon</span>
                    <div class="d-flex align-items-center justify-content-between">
                        <h3>${result.computer.lab || 'Belirtilmemiş'}</h3>
                        <div class="metric-icon"><i class="fas fa-map-marker-alt"></i></div>
                    </div>
                    <p>Bölüm ve laboratuvar adı.</p>
                </div>
            </div>
            <div class="card-soft card-body">
                <div class="section-title mb-4">
                    <div>
                        <h5>Zimmetli Bilgisayar Detayları</h5>
                        <p class="text-muted mb-0">Cihazınızın donanım ve bağlantı bilgileri buradan takip edebilirsiniz.</p>
                    </div>
                    <span class="status-chip ${result.computer.hasHdmi ? 'success' : 'danger'}">${result.computer.hasHdmi ? 'HDMI destekli' : 'HDMI yok'}</span>
                </div>
                <div class="row g-3">
                    <div class="col-md-6">
                        <div class="card border-0 bg-light p-3">
                            <strong>Marka</strong>
                            <p class="mb-0">${result.computer.brand}</p>
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="card border-0 bg-light p-3">
                            <strong>İşlemci</strong>
                            <p class="mb-0">${result.computer.processor}</p>
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="card border-0 bg-light p-3">
                            <strong>RAM</strong>
                            <p class="mb-0">${result.computer.ram}</p>
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="card border-0 bg-light p-3">
                            <strong>Veyon</strong>
                            <p class="mb-0">${result.computer.hasVeyon ? 'Evet' : 'Hayır'}</p>
                        </div>
                    </div>
                </div>
            </div>
        `;
    } catch (error) {
        content.innerHTML = '<div class="alert alert-warning">Zimmetli bilgisayar bilgisi bulunamadı veya yetkiniz yok.</div>';
    }
}
