#ifndef SYS_TIME_H
#define SYS_TIME_H

#include <time.h>
#include <windows.h>

#ifdef __cplusplus
extern "C" {
#endif

    struct timeval {
        long tv_sec;
        long tv_usec;
    };

    static inline int gettimeofday(struct timeval* tp, void* tzp) {
        // Игнорируем tzp (timezone)
        FILETIME ft;
        ULARGE_INTEGER li;
        UINT64 tt;

        GetSystemTimeAsFileTime(&ft);
        li.LowPart = ft.dwLowDateTime;
        li.HighPart = ft.dwHighDateTime;
        // Преобразование из времени Windows (100-наносекундные интервалы с 1 января 1601)
        // во время Unix (секунды с 1 января 1970)
        tt = (li.QuadPart - 116444736000000000ULL) / 10;
        tp->tv_sec = (long)(tt / 1000000);
        tp->tv_usec = (long)(tt % 1000000);
        return 0;
    }

#ifdef __cplusplus
}
#endif

#endif // SYS_TIME_H