// This file is part of Flamenco
// Copyright 2026 Canonical Ltd.
// This program is free software: you can redistribute it and/or modify it under the terms of the
// GNU General Public License version 3, as published by the Free Software Foundation.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without
// even the implied warranties of MERCHANTABILITY, SATISFACTORY QUALITY, or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with this program.
// If not, see <http://www.gnu.org/licenses/>.

// LD_PRELOAD shim that makes the openat2(2) syscall report ENOSYS.
//
// Since tar 1.35+dfsg-3ubuntu0.2 (CVE-2025-45582), GNU tar extracts every member through gnulib's
// openat2() wrapper, which calls syscall(SYS_openat2, ...) and only falls back to its openat()-based
// emulation when the kernel reports ENOSYS. snapd's seccomp template denies openat2 with EPERM
// (`~openat2`), so inside the snap tar fails with "Cannot mkdir/open: Permission denied" for every
// nested path. This affects both flamenco's own orig tarball extraction and dpkg-source.
//
// Reporting ENOSYS makes gnulib use its userspace emulation, which still enforces RESOLVE_BENEATH,
// so the CVE-2025-45582 path traversal protection is preserved.
//
// This can be removed once snapd returns ENOSYS for openat2 (or allows it).

#define _GNU_SOURCE
#include <dlfcn.h>
#include <errno.h>
#include <stdarg.h>
#include <stddef.h>
#include <sys/syscall.h>

typedef long (*syscall_fn)(long, ...);

long syscall(long number, ...)
{
    static syscall_fn real_syscall = NULL;
    va_list ap;
    long a0, a1, a2, a3, a4, a5;

#ifdef SYS_openat2
    if (number == SYS_openat2)
    {
        errno = ENOSYS;
        return -1;
    }
#endif

    if (real_syscall == NULL)
    {
        real_syscall = (syscall_fn)dlsym(RTLD_NEXT, "syscall");
        if (real_syscall == NULL)
        {
            errno = ENOSYS;
            return -1;
        }
    }

    // syscall(2) takes at most six arguments; forwarding all six is the conventional approach for
    // this kind of shim on the supported architectures (amd64, arm64).
    va_start(ap, number);
    a0 = va_arg(ap, long);
    a1 = va_arg(ap, long);
    a2 = va_arg(ap, long);
    a3 = va_arg(ap, long);
    a4 = va_arg(ap, long);
    a5 = va_arg(ap, long);
    va_end(ap);

    return real_syscall(number, a0, a1, a2, a3, a4, a5);
}
