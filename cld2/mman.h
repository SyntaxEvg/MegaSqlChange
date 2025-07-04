#ifndef SYS_MMAN_H
#define SYS_MMAN_H

#include <windows.h>
#include <errno.h>
#include <io.h>

#ifdef __cplusplus
extern "C" {
#endif

#define PROT_READ     0x1
#define PROT_WRITE    0x2
#define PROT_EXEC     0x4
#define PROT_NONE     0x0

#define MAP_SHARED    0x01
#define MAP_PRIVATE   0x02
#define MAP_ANONYMOUS 0x20
#define MAP_ANON      MAP_ANONYMOUS
#define MAP_FAILED    ((void *) -1)

    static inline void* mmap(void* addr, size_t length, int prot, int flags, int fd, off_t offset) {
        HANDLE mapping = NULL;
        DWORD protect = 0;
        DWORD access = 0;
        void* map = NULL;

        if (prot & PROT_WRITE) {
            protect = PAGE_READWRITE;
            access = FILE_MAP_WRITE;
        }
        else if (prot & PROT_READ) {
            protect = PAGE_READONLY;
            access = FILE_MAP_READ;
        }

        if (flags & MAP_ANONYMOUS) {
            mapping = CreateFileMapping(INVALID_HANDLE_VALUE, NULL, protect, 0, length, NULL);
        }
        else {
            mapping = CreateFileMapping((HANDLE)_get_osfhandle(fd), NULL, protect, 0, 0, NULL);
        }

        if (mapping == NULL) {
            return MAP_FAILED;
        }

        map = MapViewOfFile(mapping, access, 0, offset, length);
        CloseHandle(mapping);

        if (map == NULL) {
            return MAP_FAILED;
        }

        return map;
    }

    static inline int munmap(void* addr, size_t length) {
        if (UnmapViewOfFile(addr)) {
            return 0;
        }
        return -1;
    }

#ifdef __cplusplus
}
#endif

#endif // SYS_MMAN_H