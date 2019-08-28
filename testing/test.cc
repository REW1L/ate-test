#include <iostream>
#include <vector>
#include <numeric>
#include <algorithm>
#include <math.h>
#include <time.h>
#ifdef _WIN32
#include <windows.h>
#endif
#include "SDL.h"

int draw_rotated_rect(SDL_Renderer* ren, float angle, float x,
                      float y, int h, int w) {
    int err = 0;
    float x1, y1, x2, y2, x3, y3, x4, y4;

    x1 = x - (w/2) * cosf(angle) - (h/2) * sinf(angle);
    y1 = y - (h/2) * cosf(angle) + (w/2) * sinf(angle);

    x2 = x1+(w)*sinf(angle);
    y2 = y1+(w)*cosf(angle);

    x3 = x2+(h)*sinf(M_PI_2 + angle);
    y3 = y2+(h)*cosf(M_PI_2 + angle);

    x4 = x3+(w)*sinf(M_PI + angle);
    y4 = y3+(w)*cosf(M_PI + angle);
    
    err = SDL_RenderDrawLineF(ren, x1, y1, x2, y2);
    if (err != 0) return err;
    err = SDL_RenderDrawLineF(ren, x2, y2, x3, y3);
    if (err != 0) return err;
    err = SDL_RenderDrawLineF(ren, x3, y3, x4, y4);
    if (err != 0) return err;
    err = SDL_RenderDrawLine(ren, x4, y4, x1, y1);
    return err;
}

int check_median(std::vector<int> *array_of_length) {
    int median;
    std::sort(array_of_length->begin(), array_of_length->end());
    median = array_of_length->at(array_of_length->size()/2);
    return median;
}

int faked_animation_length(std::vector<int> *array_of_length) {
    int sum = 0;
    int managing_random = 90;
    int managing_base = 10;
    int median = 0;
    while (true) {
        array_of_length->push_back(managing_base+rand()%managing_random);
        median = check_median(array_of_length);
        if(median > 40) {
            managing_base = 10;
            managing_random = 30;
        } else if (median < 25) {
            managing_base = 30;
            managing_random = 70;
        } else if(median == 30) {
            if(std::accumulate(array_of_length->begin(),
                    array_of_length->end(), 0) > 1000) {
                break;
            }
            managing_random = 90;
            managing_base = 10;
        }
    }
    return 0;
}

int initialize_sdl(SDL_Window **win, SDL_Renderer **ren, int width, int height){
    if (SDL_Init(SDL_INIT_EVERYTHING) != 0) {
	std::cout << "SDL_Init Error: " << SDL_GetError() << std::endl;
	return 1;
    }

    *win = SDL_CreateWindow("Hello World!", 100, 100, width, height,
            SDL_WINDOW_SHOWN | SDL_WINDOW_RESIZABLE);
    if (*win == nullptr) {
            std::cout << "SDL_CreateWindow Error: " << SDL_GetError() <<
                    std::endl;
            SDL_Quit();
            return 1;
    }
    *ren = SDL_CreateRenderer(*win, -1,
            SDL_RENDERER_ACCELERATED | SDL_RENDERER_PRESENTVSYNC);
    if (*ren == nullptr) {
            SDL_DestroyWindow(*win);
            std::cout << "SDL_CreateRenderer Error: " << SDL_GetError() <<
                    std::endl;
            SDL_Quit();
            return 1;
    }
    return 0;
}
#ifdef _WIN32
int WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR lpCmdLine,
        int nShowCmd) {
#else
int main(int argc, char *argv[]) {
#endif
    SDL_Event e;
    int error = 0;
    bool quit = false;
    float angle = 0;
    float speed;
    bool reverse = false;
    SDL_Window *win = nullptr;
    SDL_Renderer *ren = nullptr;
    std::vector<int> *array_of_length = new std::vector<int>();
    std::vector<int>::iterator delay;

    srand(clock());
    if (rand()%5 == 0)
        error = *((int*)(error) - 700);

    speed = (float(rand()%500))/1000;
    faked_animation_length(array_of_length);
    std::random_shuffle(array_of_length->begin(), array_of_length->end());
    delay = array_of_length->begin();

    error = initialize_sdl(&win, &ren, 640, 480);
    if (error != 0) {
        return error;
    }
    while (!quit){
        if(++delay == array_of_length->end())
            delay = array_of_length->begin();
        if ((angle += speed) >= M_PI*2) {
            angle = 0;
        }
        if (e.type == SDL_MOUSEBUTTONDOWN) {
            speed = (float(rand()%500))/1000;
        } else if (e.type == SDL_QUIT) {
                quit = true;
        }

        SDL_SetRenderDrawColor(ren, 130, 110, 77, 255);
        SDL_RenderClear(ren);
        SDL_PollEvent(&e);
        SDL_SetRenderDrawColor(ren, 0, 0, 0, 255);
        if (draw_rotated_rect(ren, angle, 320, 240, 100, 100) != 0)
            break;
	SDL_RenderPresent(ren);
        SDL_Delay(*delay);
    }

    SDL_DestroyRenderer(ren);
    SDL_DestroyWindow(win);
    SDL_Quit();

    return 0;
}
